using CabinConnect.Domain.Exceptions;

namespace CabinConnect.Domain.Rates;

public static class NightlyRateCalculator
{
    public static RateBreakdown Calculate(
        DateOnly checkIn,
        DateOnly checkOut,
        decimal baseRate,
        IReadOnlyList<SeasonalRate> seasonalRates)
    {
        if (checkOut <= checkIn)
            throw new DomainValidationException(
                "Check-out must be after check-in. Minimum stay is 1 night.");

        var lineItems = new List<RateLineItem>();
        var night = checkIn;

        while (night < checkOut)
        {
            lineItems.Add(ResolveNight(night, baseRate, seasonalRates));
            night = night.AddDays(1);
        }

        return new RateBreakdown(lineItems);
    }

    private static RateLineItem ResolveNight(
        DateOnly night,
        decimal baseRate,
        IReadOnlyList<SeasonalRate> seasonalRates)
    {
        var candidates = seasonalRates
            .Where(sr => sr.StartDate <= night && night < sr.EndDate)
            .ToList();

        if (candidates.Count == 0)
            return new RateLineItem(night, baseRate, RateType.BaseRate, null);

        // EC-005: narrowest range wins; equal length → higher rate
        var winner = candidates
            .OrderBy(sr => sr.EndDate.DayNumber - sr.StartDate.DayNumber)
            .ThenByDescending(sr => sr.Rate)
            .First();

        return new RateLineItem(night, winner.Rate, RateType.SeasonalRate, winner.Name);
    }
}
