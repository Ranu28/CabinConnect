using CabinConnect.Domain.Holds;
using Dapper;
using Npgsql;

namespace CabinConnect.Infrastructure.Holds;

public sealed class HoldRepository : IHoldRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public HoldRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Hold> PlaceHoldAsync(
        Guid cabinId, Guid guestId, DateOnly checkIn, DateOnly checkOut,
        CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx  = await conn.BeginTransactionAsync(ct);
        try
        {
            // Lock the cabin row to prevent concurrent holds on the same cabin (EC-001).
            var lockedId = await conn.QuerySingleOrDefaultAsync<Guid?>(
                "SELECT id FROM cabins WHERE id = @CabinId AND is_published = true FOR UPDATE",
                new { CabinId = cabinId }, transaction: tx);

            if (lockedId is null)
                throw new CabinNotFoundException(cabinId);

            // Check for conflicting Confirmed/Pending Bookings.
            var hasBookingConflict = await conn.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS(
                    SELECT 1 FROM bookings b
                    WHERE  b.cabin_id  = @CabinId
                      AND  b.check_in  < @CheckOut
                      AND  b.check_out > @CheckIn
                      AND  b.status    = ANY(ARRAY['Confirmed', 'Pending'])
                )
                """,
                new { CabinId = cabinId, CheckIn = checkIn, CheckOut = checkOut },
                transaction: tx);

            if (hasBookingConflict)
                throw new CabinUnavailableException();

            // Check for conflicting Active Holds (unexpired).
            var hasHoldConflict = await conn.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS(
                    SELECT 1 FROM holds h
                    WHERE  h.cabin_id  = @CabinId
                      AND  h.check_in  < @CheckOut
                      AND  h.check_out > @CheckIn
                      AND  h.status    = 'Active'
                      AND  h.expires_at > now()
                )
                """,
                new { CabinId = cabinId, CheckIn = checkIn, CheckOut = checkOut },
                transaction: tx);

            if (hasHoldConflict)
                throw new CabinUnavailableException();

            // Check for overlapping Blackout Dates (EC-004).
            var hasBlackout = await conn.ExecuteScalarAsync<bool>(
                """
                SELECT EXISTS(
                    SELECT 1 FROM blackout_dates bd
                    WHERE  bd.cabin_id   = @CabinId
                      AND  bd.start_date < @CheckOut
                      AND  bd.end_date   > @CheckIn
                )
                """,
                new { CabinId = cabinId, CheckIn = checkIn, CheckOut = checkOut },
                transaction: tx);

            if (hasBlackout)
                throw new CabinUnavailableException();

            // Auto-cancel any previous Active Hold for this Guest (EC-011).
            await conn.ExecuteAsync(
                "UPDATE holds SET status = 'Cancelled' WHERE guest_id = @GuestId AND status = 'Active'",
                new { GuestId = guestId }, transaction: tx);

            // Insert the new Hold; expiresAt is always server-side UTC now + 15 min.
            var row = await conn.QuerySingleAsync<HoldRow>(
                """
                INSERT INTO holds (cabin_id, guest_id, check_in, check_out, expires_at, status)
                VALUES (@CabinId, @GuestId, @CheckIn, @CheckOut, now() + interval '15 minutes', 'Active')
                RETURNING id AS Id, expires_at AS ExpiresAt
                """,
                new { CabinId = cabinId, GuestId = guestId, CheckIn = checkIn, CheckOut = checkOut },
                transaction: tx);

            await tx.CommitAsync(ct);

            return new Hold
            {
                Id        = row.Id,
                CabinId   = cabinId,
                GuestId   = guestId,
                CheckIn   = checkIn,
                CheckOut  = checkOut,
                ExpiresAt = row.ExpiresAt,
                Status    = HoldStatus.Active
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private sealed class HoldRow
    {
        public Guid            Id        { get; set; }
        public DateTimeOffset  ExpiresAt { get; set; }
    }
}
