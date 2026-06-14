using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Common;
using CabinConnect.Domain.Rates;

namespace CabinConnect.Domain.Search;

public sealed class CabinSearchService
{
    private readonly ICabinSearchRepository _repository;

    public CabinSearchService(ICabinSearchRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<CabinSearchResult>> SearchAsync(
        CabinSearchQuery query, CancellationToken ct = default)
    {
        var data = await _repository.GetPublishedCabinsWithAvailabilityDataAsync(
            query.CheckIn, query.CheckOut, ct);

        var results = new List<CabinSearchResult>();

        foreach (var d in data)
        {
            // AC-11: only published cabins (repo pre-filters, guard here too)
            if (!d.Cabin.IsPublished) continue;

            // AC-1: exclude if a Confirmed or Pending booking overlaps the range
            if (d.OverlappingBookings.Any(b =>
                    b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
                continue;

            // AC-2: exclude if a blackout date overlaps the range (EC-004)
            if (d.OverlappingBlackouts.Count > 0) continue;

            // AC-4: guest count filter
            if (query.Guests.HasValue && d.Cabin.MaxGuests < query.Guests.Value) continue;

            // AC-3: calculate price using CSA-001
            var breakdown = NightlyRateCalculator.Calculate(
                query.CheckIn, query.CheckOut, d.Cabin.BaseRate, d.Cabin.SeasonalRates);

            var totalPrice = breakdown.TotalPrice;

            // AC-5: price range filter
            if (query.MinPrice.HasValue && totalPrice < query.MinPrice.Value) continue;
            if (query.MaxPrice.HasValue && totalPrice > query.MaxPrice.Value) continue;

            // Amenity filter — AND operation
            if (query.Amenities is { Count: > 0 } required)
                if (!required.All(a => d.Cabin.Amenities.Contains(a, StringComparer.OrdinalIgnoreCase)))
                    continue;

            results.Add(new CabinSearchResult(
                CabinId: d.Cabin.Id,
                Name: d.Cabin.Name,
                Description: d.Cabin.Description,
                ImageUrl: d.Cabin.ImageUrl,
                MaxGuests: d.Cabin.MaxGuests,
                Amenities: d.Cabin.Amenities,
                Location: d.Cabin.Location,
                PriceBreakdown: GroupLineItems(breakdown.LineItems),
                TotalPrice: totalPrice,
                Currency: d.Cabin.Currency));
        }

        // AC-6: sort ascending by total price
        results.Sort((a, b) => a.TotalPrice.CompareTo(b.TotalPrice));

        var totalCount = results.Count;

        // AC-12: paginate
        var items = results
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResult<CabinSearchResult>(items, query.Page, query.PageSize, totalCount);
    }

    private static List<RateLineItemResult> GroupLineItems(IReadOnlyList<RateLineItem> lineItems)
    {
        var groups = new List<RateLineItemResult>();
        int i = 0;
        while (i < lineItems.Count)
        {
            var current = lineItems[i];
            int count = 1;
            while (i + count < lineItems.Count
                   && lineItems[i + count].Rate == current.Rate
                   && lineItems[i + count].RateType == current.RateType
                   && lineItems[i + count].SeasonalRateName == current.SeasonalRateName)
                count++;

            var rateLabel = current.RateType == RateType.BaseRate
                ? "Base Rate"
                : current.SeasonalRateName ?? "Seasonal Rate";
            var nightsWord = count == 1 ? "night" : "nights";
            var label = $"{count} {nightsWord} × {current.Rate:N2} ({rateLabel})";

            groups.Add(new RateLineItemResult(
                Label: label,
                Nights: count,
                RatePerNight: current.Rate,
                RateType: current.RateType == RateType.BaseRate ? "BaseRate" : "SeasonalRate",
                Subtotal: current.Rate * count));

            i += count;
        }
        return groups;
    }
}
