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

    public Task<Cabins.Cabin?> GetCabinByIdAsync(Guid cabinId, CancellationToken ct = default)
        => _repository.GetCabinByIdAsync(cabinId, ct);

    public async Task<PagedResult<CabinSearchResult>> SearchAsync(
        CabinSearchQuery query, CancellationToken ct = default)
    {
        var (page, totalCount) = await _repository.SearchAvailablePageAsync(query, ct);

        var items = page.Select(cabin =>
        {
            var breakdown = NightlyRateCalculator.Calculate(
                query.CheckIn, query.CheckOut, cabin.BaseRate, cabin.SeasonalRates);

            return new CabinSearchResult(
                CabinId:        cabin.Id,
                Name:           cabin.Name,
                Description:    cabin.Description,
                ImageUrl:       cabin.ImageUrl,
                MaxGuests:      cabin.MaxGuests,
                Amenities:      cabin.Amenities,
                Location:       cabin.Location,
                PriceBreakdown: GroupLineItems(breakdown.LineItems),
                TotalPrice:     breakdown.TotalPrice,
                Currency:       cabin.Currency);
        }).ToList();

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
                   && lineItems[i + count].Rate          == current.Rate
                   && lineItems[i + count].RateType      == current.RateType
                   && lineItems[i + count].SeasonalRateName == current.SeasonalRateName)
                count++;

            var rateLabel = current.RateType == RateType.BaseRate
                ? "Base Rate"
                : current.SeasonalRateName ?? "Seasonal Rate";
            var nightsWord = count == 1 ? "night" : "nights";

            groups.Add(new RateLineItemResult(
                Label:        $"{count} {nightsWord} × {current.Rate:N2} ({rateLabel})",
                Nights:       count,
                RatePerNight: current.Rate,
                RateType:     current.RateType == RateType.BaseRate ? "BaseRate" : "SeasonalRate",
                Subtotal:     current.Rate * count));

            i += count;
        }
        return groups;
    }
}
