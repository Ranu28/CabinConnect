namespace CabinConnect.Domain.Search;

public sealed record CabinSearchQuery(
    DateOnly CheckIn,
    DateOnly CheckOut,
    int? Guests = null,
    IReadOnlyList<string>? Amenities = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int Page = 1,
    int PageSize = 20);
