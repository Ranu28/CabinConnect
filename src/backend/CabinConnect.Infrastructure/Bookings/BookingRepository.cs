using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Common;
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

    public async Task CancelBookingAsync(Guid bookingId, Guid guestId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        // Include guest_id in the filter so a booking owned by another Guest returns null —
        // the caller gets 404 for both "not found" and "not owned", preventing info leaks (EC-007).
        var row = await conn.QuerySingleOrDefaultAsync<BookingStatusRow>(
            "SELECT id AS Id, status AS Status FROM bookings WHERE id = @BookingId AND guest_id = @GuestId",
            new { BookingId = bookingId, GuestId = guestId });

        // AC-6/AC-8: null means not found OR not owned — both surface as 404 (EC-007).
        if (row is null)
            throw new BookingNotFoundException(bookingId);

        var status = Enum.Parse<BookingStatus>(row.Status);

        // AC-5: already Cancelled — idempotent, no update needed.
        if (status == BookingStatus.Cancelled)
            return;

        // AC-3/AC-4: terminal states cannot be cancelled.
        if (status is BookingStatus.Completed or BookingStatus.NoShow)
            throw new BookingCannotBeCancelledException(status);

        // AC-1/AC-2: cancel Confirmed or Pending booking.
        await conn.ExecuteAsync(
            "UPDATE bookings SET status = 'Cancelled', updated_at = now() WHERE id = @BookingId",
            new { BookingId = bookingId });
    }

    public async Task<PagedResult<GuestBookingItem>> GetGuestBookingsAsync(
        Guid guestId, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var rows = (await conn.QueryAsync<BookingListRow>(
            """
            SELECT b.id           AS BookingId,
                   b.cabin_id     AS CabinId,
                   c.name         AS CabinName,
                   b.check_in     AS CheckIn,
                   b.check_out    AS CheckOut,
                   b.total_price  AS TotalPrice,
                   b.currency     AS Currency,
                   b.status       AS Status,
                   COUNT(*) OVER() AS TotalCount
            FROM   bookings b
            JOIN   cabins   c ON c.id = b.cabin_id
            WHERE  b.guest_id = @GuestId
            ORDER  BY b.check_in DESC
            LIMIT  @PageSize OFFSET ((@Page - 1) * @PageSize)
            """,
            new { GuestId = guestId, Page = page, PageSize = pageSize })).AsList();

        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;
        var items = rows.Select(ToItem).ToList();
        return new PagedResult<GuestBookingItem>(items, page, pageSize, totalCount);
    }

    public async Task<GuestBookingItem?> GetGuestBookingByIdAsync(
        Guid bookingId, Guid guestId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var row = await conn.QuerySingleOrDefaultAsync<BookingListRow>(
            """
            SELECT b.id          AS BookingId,
                   b.cabin_id    AS CabinId,
                   c.name        AS CabinName,
                   b.check_in    AS CheckIn,
                   b.check_out   AS CheckOut,
                   b.total_price AS TotalPrice,
                   b.currency    AS Currency,
                   b.status      AS Status,
                   0             AS TotalCount
            FROM   bookings b
            JOIN   cabins   c ON c.id = b.cabin_id
            WHERE  b.id = @BookingId AND b.guest_id = @GuestId
            """,
            new { BookingId = bookingId, GuestId = guestId });

        return row is null ? null : ToItem(row);
    }

    public async Task<PagedResult<HostBookingItem>> GetHostBookingsAsync(
        Guid hostId, BookingStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        // AC-2: optional status filter; null means all statuses.
        // AC-3: JOIN cabins ON host_id = @HostId restricts to this Host's cabins only.
        var rows = (await conn.QueryAsync<HostBookingListRow>(
            """
            SELECT b.id           AS BookingId,
                   b.cabin_id     AS CabinId,
                   c.name         AS CabinName,
                   b.guest_id     AS GuestId,
                   b.check_in     AS CheckIn,
                   b.check_out    AS CheckOut,
                   b.total_price  AS TotalPrice,
                   b.currency     AS Currency,
                   b.status       AS Status,
                   COUNT(*) OVER() AS TotalCount
            FROM   bookings b
            JOIN   cabins   c ON c.id = b.cabin_id AND c.host_id = @HostId
            WHERE  (@Status IS NULL OR b.status = @Status)
            ORDER  BY b.check_in DESC
            LIMIT  @PageSize OFFSET ((@Page - 1) * @PageSize)
            """,
            new
            {
                HostId   = hostId,
                Status   = status?.ToString(),
                Page     = page,
                PageSize = pageSize
            })).AsList();

        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;
        var items = rows.Select(r => new HostBookingItem(
            r.BookingId, r.CabinId, r.CabinName, r.GuestId,
            r.CheckIn, r.CheckOut, r.TotalPrice, r.Currency,
            Enum.Parse<BookingStatus>(r.Status, ignoreCase: true))).ToList();

        return new PagedResult<HostBookingItem>(items, page, pageSize, totalCount);
    }

    private sealed class HostBookingListRow
    {
        public Guid     BookingId  { get; set; }
        public Guid     CabinId    { get; set; }
        public string   CabinName  { get; set; } = "";
        public Guid     GuestId    { get; set; }
        public DateOnly CheckIn    { get; set; }
        public DateOnly CheckOut   { get; set; }
        public decimal  TotalPrice { get; set; }
        public string   Currency   { get; set; } = "";
        public string   Status     { get; set; } = "";
        public int      TotalCount { get; set; }
    }

    private static GuestBookingItem ToItem(BookingListRow r) =>
        new(r.BookingId, r.CabinId, r.CabinName,
            r.CheckIn, r.CheckOut, r.TotalPrice, r.Currency,
            Enum.Parse<BookingStatus>(r.Status, ignoreCase: true));

    private sealed class BookingListRow
    {
        public Guid     BookingId  { get; set; }
        public Guid     CabinId    { get; set; }
        public string   CabinName  { get; set; } = "";
        public DateOnly CheckIn    { get; set; }
        public DateOnly CheckOut   { get; set; }
        public decimal  TotalPrice { get; set; }
        public string   Currency   { get; set; } = "";
        public string   Status     { get; set; } = "";
        public int      TotalCount { get; set; }
    }

    private sealed class BookingStatusRow
    {
        public Guid   Id     { get; set; }
        public string Status { get; set; } = "";
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
