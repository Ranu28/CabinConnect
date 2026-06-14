using CabinConnect.Domain.Bookings;
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

    public async Task<IReadOnlyList<CabinWithAvailabilityData>> GetPublishedCabinsWithAvailabilityDataAsync(
        DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var cabinRows = (await conn.QueryAsync<CabinRow>(
            """
            SELECT id,
                   name,
                   description,
                   image_url    AS ImageUrl,
                   max_guests   AS MaxGuests,
                   base_rate    AS BaseRate,
                   currency,
                   amenities,
                   location_lat AS LocationLat,
                   location_lng AS LocationLng
            FROM   cabins
            WHERE  is_published = true
            """)).AsList();

        if (cabinRows.Count == 0)
            return [];

        var cabinIds = cabinRows.Select(c => c.Id).ToArray();

        // Queries run sequentially on one connection — ADO.NET connections do not
        // support concurrent commands on the same instance.
        var rateRows = await conn.QueryAsync<SeasonalRateRow>(
            """
            SELECT id,
                   cabin_id   AS CabinId,
                   name,
                   start_date AS StartDate,
                   end_date   AS EndDate,
                   rate
            FROM   seasonal_rates
            WHERE  cabin_id = ANY(@CabinIds)
            """, new { CabinIds = cabinIds });

        // Overlap condition: booking.check_in < @CheckOut AND booking.check_out > @CheckIn
        var bookingRows = await conn.QueryAsync<BookingRow>(
            """
            SELECT id,
                   cabin_id  AS CabinId,
                   check_in  AS CheckIn,
                   check_out AS CheckOut,
                   status
            FROM   bookings
            WHERE  cabin_id = ANY(@CabinIds)
              AND  check_in  < @CheckOut
              AND  check_out > @CheckIn
              AND  status = ANY(@ActiveStatuses)
            """, new
            {
                CabinIds      = cabinIds,
                CheckIn       = checkIn,
                CheckOut      = checkOut,
                ActiveStatuses = new[] { "Confirmed", "Pending" }
            });

        var blackoutRows = await conn.QueryAsync<BlackoutRow>(
            """
            SELECT id,
                   cabin_id   AS CabinId,
                   start_date AS StartDate,
                   end_date   AS EndDate
            FROM   blackout_dates
            WHERE  cabin_id = ANY(@CabinIds)
              AND  start_date < @CheckOut
              AND  end_date   > @CheckIn
            """, new
            {
                CabinIds = cabinIds,
                CheckIn  = checkIn,
                CheckOut = checkOut
            });

        var ratesByCabin = rateRows
            .GroupBy(r => r.CabinId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<SeasonalRate>)g
                    .Select(r => new SeasonalRate(r.StartDate, r.EndDate, r.Rate, r.Name))
                    .ToList());

        var bookingsByCabin = bookingRows
            .GroupBy(b => b.CabinId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Booking>)g
                    .Select(b => new Booking
                    {
                        Id       = b.Id,
                        CabinId  = b.CabinId,
                        CheckIn  = b.CheckIn,
                        CheckOut = b.CheckOut,
                        Status   = Enum.Parse<BookingStatus>(b.Status)
                    })
                    .ToList());

        var blackoutsByCabin = blackoutRows
            .GroupBy(b => b.CabinId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<BlackoutDate>)g
                    .Select(b => new BlackoutDate
                    {
                        Id        = b.Id,
                        CabinId   = b.CabinId,
                        StartDate = b.StartDate,
                        EndDate   = b.EndDate
                    })
                    .ToList());

        return cabinRows
            .Select(c => new CabinWithAvailabilityData
            {
                Cabin = new Cabin
                {
                    Id            = c.Id,
                    Name          = c.Name,
                    Description   = c.Description,
                    ImageUrl      = c.ImageUrl,
                    MaxGuests     = c.MaxGuests,
                    BaseRate      = c.BaseRate,
                    Currency      = c.Currency,
                    Amenities     = c.Amenities ?? [],
                    Location      = c.LocationLat.HasValue && c.LocationLng.HasValue
                                        ? new CabinLocation(c.LocationLat.Value, c.LocationLng.Value)
                                        : null,
                    SeasonalRates = ratesByCabin.GetValueOrDefault(c.Id, []),
                    IsPublished   = true
                },
                OverlappingBookings  = bookingsByCabin.GetValueOrDefault(c.Id, []),
                OverlappingBlackouts = blackoutsByCabin.GetValueOrDefault(c.Id, [])
            })
            .ToList();
    }

    // Property-based classes (not positional records) so Dapper can map columns
    // via property setters — constructor-based mapping breaks for array and nullable types.

    private sealed class CabinRow
    {
        public Guid     Id          { get; set; }
        public string   Name        { get; set; } = "";
        public string   Description { get; set; } = "";
        public string?  ImageUrl    { get; set; }
        public int      MaxGuests   { get; set; }
        public decimal  BaseRate    { get; set; }
        public string   Currency    { get; set; } = "";
        public string[]? Amenities  { get; set; }
        public double?  LocationLat { get; set; }
        public double?  LocationLng { get; set; }
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

    private sealed class BookingRow
    {
        public Guid     Id       { get; set; }
        public Guid     CabinId  { get; set; }
        public DateOnly CheckIn  { get; set; }
        public DateOnly CheckOut { get; set; }
        public string   Status   { get; set; } = "";
    }

    private sealed class BlackoutRow
    {
        public Guid     Id        { get; set; }
        public Guid     CabinId   { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate   { get; set; }
    }
}
