namespace CabinConnect.Domain.Search;

public sealed record RateLineItemResult(
    string Label,
    int Nights,
    decimal RatePerNight,
    string RateType,
    decimal Subtotal);
