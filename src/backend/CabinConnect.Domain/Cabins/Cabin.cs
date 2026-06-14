using CabinConnect.Domain.Rates;

namespace CabinConnect.Domain.Cabins;

public sealed class Cabin
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? ImageUrl { get; init; }
    public required int MaxGuests { get; init; }
    public required IReadOnlyList<string> Amenities { get; init; }
    public required decimal BaseRate { get; init; }
    public required string Currency { get; init; }
    public CabinLocation? Location { get; init; }
    public required IReadOnlyList<SeasonalRate> SeasonalRates { get; init; }
    public required bool IsPublished { get; init; }
}
