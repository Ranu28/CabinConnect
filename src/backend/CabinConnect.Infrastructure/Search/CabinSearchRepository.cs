using CabinConnect.Domain.Cabins;
using CabinConnect.Domain.Rates;
using CabinConnect.Domain.Search;
using Dapper;
using Npgsql;

namespace CabinConnect.Infrastructure.Search;

public sealed class CabinSearchRepository : ICabinSearchRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public CabinSearchRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<(IReadOnlyList<Cabin> Page, int TotalCount)> SearchAvailablePageAsync(
        CabinSearchQuery query, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var nights    = query.CheckOut.DayNumber - query.CheckIn.DayNumber;
        var amenities = query.Amenities is { Count: > 0 }
            ? query.Amenities.ToArray()
            : Array.Empty<string>();

        // Common CTEs used by both the page query and the fallback count query.
        // nights:     one row per night in the stay (integer offsets from check-in).
        // available:  published cabins passing availability + filter criteria.
        // priced:     each available cabin annotated with its total_price for the stay.
        //             Fast path: base_rate × nights when no seasonal rates overlap.
        //             Slow path: per-night rate resolution matching NightlyRateCalculator (EC-005).
        const string cteSql = """
            WITH nights AS (
                SELECT (@CheckIn + s.i) AS night
                FROM   generate_series(0, @Nights - 1) AS s(i)
            ),
            available AS (
                SELECT c.id, c.name, c.description, c.image_url, c.max_guests,
                       c.base_rate, c.currency, c.amenities, c.location_lat, c.location_lng
                FROM   cabins c
                WHERE  c.is_published = true
                  AND  (@Guests    IS NULL OR c.max_guests >= @Guests)
                  AND  (array_length(@Amenities::text[], 1) IS NULL
                            OR c.amenities @> @Amenities::text[])
                  AND  NOT EXISTS (
                           SELECT 1 FROM bookings b
                           WHERE  b.cabin_id  = c.id
                             AND  b.check_in  < @CheckOut
                             AND  b.check_out > @CheckIn
                             AND  b.status    = ANY(ARRAY['Confirmed', 'Pending'])
                       )
                  AND  NOT EXISTS (
                           SELECT 1 FROM blackout_dates bd
                           WHERE  bd.cabin_id  = c.id
                             AND  bd.start_date < @CheckOut
                             AND  bd.end_date   > @CheckIn
                       )
            ),
            priced AS (
                SELECT a.*,
                       CASE
                           WHEN NOT EXISTS (
                               SELECT 1 FROM seasonal_rates sr
                               WHERE  sr.cabin_id   = a.id
                                 AND  sr.start_date < @CheckOut
                                 AND  sr.end_date   > @CheckIn
                           ) THEN
                               a.base_rate * @Nights
                           ELSE (
                               SELECT SUM(COALESCE(
                                   (SELECT sr2.rate FROM seasonal_rates sr2
                                    WHERE  sr2.cabin_id   = a.id
                                      AND  sr2.start_date <= n.night
                                      AND  n.night < sr2.end_date
                                    ORDER BY (sr2.end_date - sr2.start_date) ASC,
                                             sr2.rate DESC
                                    LIMIT 1),
                                   a.base_rate
                               ))
                               FROM nights n
                           )
                       END AS total_price
                FROM available a
            )
            """;

        var parameters = new
        {
            CheckIn   = query.CheckIn,
            CheckOut  = query.CheckOut,
            Nights    = nights,
            Guests    = query.Guests,
            Amenities = amenities,
            MinPrice  = query.MinPrice,
            MaxPrice  = query.MaxPrice,
            Page      = query.Page,
            PageSize  = query.PageSize
        };

        // Page query — returns pageSize rows with COUNT(*) OVER() for the total.
        // COUNT(*) OVER() is computed after WHERE but before LIMIT/OFFSET, so it
        // reflects the filtered result set, not just the page.
        var pageRows = (await conn.QueryAsync<CabinPageRow>(
            cteSql + """
            SELECT p.id           AS Id,
                   p.name         AS Name,
                   p.description  AS Description,
                   p.image_url    AS ImageUrl,
                   p.max_guests   AS MaxGuests,
                   p.base_rate    AS BaseRate,
                   p.currency     AS Currency,
                   p.amenities    AS Amenities,
                   p.location_lat AS LocationLat,
                   p.location_lng AS LocationLng,
                   COUNT(*) OVER() AS TotalCount
            FROM   priced p
            WHERE  (@MinPrice IS NULL OR p.total_price >= @MinPrice)
              AND  (@MaxPrice IS NULL OR p.total_price <= @MaxPrice)
            ORDER  BY p.total_price ASC
            LIMIT  @PageSize OFFSET ((@Page - 1) * @PageSize)
            """, parameters)).AsList();

        // When the page query returns rows, TotalCount comes from the window function.
        if (pageRows.Count > 0)
            return (BuildCabins(pageRows, await FetchRatesAsync(conn, pageRows)), pageRows[0].TotalCount);

        // Page beyond last (or no results at all) — run a lightweight count query so
        // callers receive the correct TotalCount rather than 0.
        var totalCount = await conn.ExecuteScalarAsync<int>(
            cteSql + """
            SELECT COUNT(*)::int
            FROM   priced p
            WHERE  (@MinPrice IS NULL OR p.total_price >= @MinPrice)
              AND  (@MaxPrice IS NULL OR p.total_price <= @MaxPrice)
            """, parameters);

        return ([], totalCount);
    }

    private static async Task<IEnumerable<SeasonalRateRow>> FetchRatesAsync(
        System.Data.IDbConnection conn, List<CabinPageRow> pageRows)
    {
        var ids = pageRows.Select(r => r.Id).ToArray();
        return await conn.QueryAsync<SeasonalRateRow>(
            """
            SELECT id         AS Id,
                   cabin_id   AS CabinId,
                   name       AS Name,
                   start_date AS StartDate,
                   end_date   AS EndDate,
                   rate       AS Rate
            FROM   seasonal_rates
            WHERE  cabin_id = ANY(@CabinIds)
            """, new { CabinIds = ids });
    }

    private static IReadOnlyList<Cabin> BuildCabins(
        List<CabinPageRow> rows, IEnumerable<SeasonalRateRow> rateRows)
    {
        var ratesByCabin = rateRows
            .GroupBy(r => r.CabinId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<SeasonalRate>)g
                    .Select(r => new SeasonalRate(r.StartDate, r.EndDate, r.Rate, r.Name))
                    .ToList());

        return rows.Select(r => new Cabin
        {
            Id            = r.Id,
            Name          = r.Name,
            Description   = r.Description,
            ImageUrl      = r.ImageUrl,
            MaxGuests     = r.MaxGuests,
            BaseRate      = r.BaseRate,
            Currency      = r.Currency,
            Amenities     = r.Amenities ?? [],
            Location      = r.LocationLat.HasValue && r.LocationLng.HasValue
                                ? new CabinLocation(r.LocationLat.Value, r.LocationLng.Value)
                                : null,
            SeasonalRates = ratesByCabin.GetValueOrDefault(r.Id, []),
            IsPublished   = true
        }).ToList();
    }

    private sealed class CabinPageRow
    {
        public Guid      Id          { get; set; }
        public string    Name        { get; set; } = "";
        public string    Description { get; set; } = "";
        public string?   ImageUrl    { get; set; }
        public int       MaxGuests   { get; set; }
        public decimal   BaseRate    { get; set; }
        public string    Currency    { get; set; } = "";
        public string[]? Amenities   { get; set; }
        public double?   LocationLat { get; set; }
        public double?   LocationLng { get; set; }
        public int       TotalCount  { get; set; }
    }

    private sealed class SeasonalRateRow
    {
        public Guid     Id        { get; set; }
        public Guid     CabinId   { get; set; }
        public string   Name      { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate   { get; set; }
        public decimal  Rate      { get; set; }
    }
}
