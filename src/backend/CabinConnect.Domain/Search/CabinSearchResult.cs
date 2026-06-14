using CabinConnect.Domain.Cabins;

namespace CabinConnect.Domain.Search;

public sealed record CabinSearchResult(
    Guid CabinId,
    string Name,
    string Description,
    string? ImageUrl,
    int MaxGuests,
    IReadOnlyList<string> Amenities,
    CabinLocation? Location,
    IReadOnlyList<RateLineItemResult> PriceBreakdown,
    decimal TotalPrice,
    string Currency);
