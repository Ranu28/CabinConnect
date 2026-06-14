namespace CabinConnect.Domain.Rates;

public sealed record RateLineItem(
    DateOnly Date,
    decimal Rate,
    RateType RateType,
    string? SeasonalRateName);
