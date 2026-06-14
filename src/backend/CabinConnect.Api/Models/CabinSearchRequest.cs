namespace CabinConnect.Api.Models;

public sealed class CabinSearchRequest
{
    public string CheckIn { get; init; } = string.Empty;
    public string CheckOut { get; init; } = string.Empty;
    public int? Guests { get; init; }
    public string[]? Amenities { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
