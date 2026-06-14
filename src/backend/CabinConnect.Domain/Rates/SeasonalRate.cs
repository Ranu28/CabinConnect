namespace CabinConnect.Domain.Rates;

public sealed record SeasonalRate(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Rate,
    string Name);
