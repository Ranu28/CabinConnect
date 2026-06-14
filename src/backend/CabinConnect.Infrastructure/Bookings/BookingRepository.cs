using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Holds;
using CabinConnect.Domain.Rates;
using Dapper;
using Npgsql;

namespace CabinConnect.Infrastructure.Bookings;

public sealed class BookingRepository : IBookingRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public BookingRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<(Booking Booking, RateBreakdown PriceBreakdown)> ConfirmHoldAsync(
        Guid holdId, Guid guestId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx  = await conn.BeginTransactionAsync(ct);
        try
        {
            // Lock the Hold row and fetch cabin base rate and currency in one query (EC-012, EC-001).
            var row = await conn.QuerySingleOrDefaultAsync<HoldWithCabinRow>(
                """
                SELECT h.id          AS HoldId,
                       h.cabin_id    AS CabinId,
                       h.guest_id    AS GuestId,
                       h.check_in    AS CheckIn,
                       h.check_out   AS CheckOut,
                       h.expires_at  AS ExpiresAt,
                       h.status      AS Status,
                       c.base_rate   AS BaseRate,
                       c.currency    AS Currency
                FROM   holds h
                JOIN   cabins c ON c.id = h.cabin_id
                WHERE  h.id = @HoldId
                FOR UPDATE OF h
                """,
                new { HoldId = holdId }, transaction: tx);

            // AC-8: hold not found
            if (row is null)
                throw new HoldNotFoundException(holdId);

            // AC-4: guest must own the Hold (EC-007) — check before revealing status
            if (row.GuestId != guestId)
                throw new HoldOwnershipException();

            // AC-3: Hold must be Active
            if (row.Status != "Active")
                throw new HoldNotActiveException();

            // AC-2: Hold must not be expired (EC-002) — validate server-side regardless of status field
            if (row.ExpiresAt < DateTimeOffset.UtcNow)
                throw new HoldExpiredException();

            // Fetch seasonal rates for the stay dates to compute the frozen price (AC-6, EC-006).
            var rateRows = await conn.QueryAsync<SeasonalRateRow>(
                """
                SELECT name       AS Name,
                       start_date AS StartDate,
                       end_date   AS EndDate,
                       rate       AS Rate
                FROM   seasonal_rates
                WHERE  cabin_id   = @CabinId
                  AND  start_date < @CheckOut
                  AND  end_date   > @CheckIn
                """,
                new { row.CabinId, row.CheckIn, row.CheckOut }, transaction: tx);

            var seasonalRates = rateRows
                .Select(r => new SeasonalRate(r.StartDate, r.EndDate, r.Rate, r.Name))
                .ToList();

            // Price frozen at confirmation time — never from client input (EC-006).
            var breakdown = NightlyRateCalculator.Calculate(
                row.CheckIn, row.CheckOut, row.BaseRate, seasonalRates);

            // AC-1: insert Booking with status=Confirmed and frozen total price.
            var bookingId = await conn.ExecuteScalarAsync<Guid>(
                """
                INSERT INTO bookings (cabin_id, guest_id, check_in, check_out, status, total_price, currency, hold_id)
                VALUES (@CabinId, @GuestId, @CheckIn, @CheckOut, 'Confirmed', @TotalPrice, @Currency, @HoldId)
                RETURNING id
                """,
                new
                {
                    row.CabinId,
                    row.GuestId,
                    row.CheckIn,
                    row.CheckOut,
                    TotalPrice = breakdown.TotalPrice,
                    row.Currency,
                    HoldId = holdId
                },
                transaction: tx);

            // Consume the Hold so it cannot be used again (EC-012).
            await conn.ExecuteAsync(
                "UPDATE holds SET status = 'Consumed' WHERE id = @HoldId",
                new { HoldId = holdId }, transaction: tx);

            await tx.CommitAsync(ct);

            var booking = new Booking
            {
                Id         = bookingId,
                CabinId    = row.CabinId,
                GuestId    = row.GuestId,
                HoldId     = holdId,
                CheckIn    = row.CheckIn,
                CheckOut   = row.CheckOut,
                Status     = BookingStatus.Confirmed,
                TotalPrice = breakdown.TotalPrice,
                Currency   = row.Currency
            };

            return (booking, breakdown);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private sealed class HoldWithCabinRow
    {
        public Guid            HoldId    { get; set; }
        public Guid            CabinId   { get; set; }
        public Guid            GuestId   { get; set; }
        public DateOnly        CheckIn   { get; set; }
        public DateOnly        CheckOut  { get; set; }
        public DateTimeOffset  ExpiresAt { get; set; }
        public string          Status    { get; set; } = "";
        public decimal         BaseRate  { get; set; }
        public string          Currency  { get; set; } = "";
    }

    private sealed class SeasonalRateRow
    {
        public string  Name      { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate   { get; set; }
        public decimal  Rate      { get; set; }
    }
}
