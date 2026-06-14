using CabinConnect.Domain.Exceptions;
using CabinConnect.Domain.Rates;
using FluentAssertions;

namespace CabinConnect.Domain.Tests.Rates;

public class NightlyRateCalculatorTests
{
    // AC-1: Base Rate only stay
    [Fact]
    public void Calculate_BaseRateOnly_ReturnsOneLineItemPerNightAndCorrectTotal()
    {
        var checkIn = new DateOnly(2026, 6, 1);
        var checkOut = new DateOnly(2026, 6, 5); // 4 nights
        const decimal baseRate = 100m;

        var result = NightlyRateCalculator.Calculate(checkIn, checkOut, baseRate, []);

        result.LineItems.Should().HaveCount(4);
        result.LineItems.Should().AllSatisfy(li =>
        {
            li.Rate.Should().Be(baseRate);
            li.RateType.Should().Be(RateType.BaseRate);
            li.SeasonalRateName.Should().BeNull();
        });
        result.TotalPrice.Should().Be(400m);
    }

    // AC-2: Seasonal Rate applies for part of the stay
    [Fact]
    public void Calculate_PartialSeasonalRate_ReturnsGroupedLineItemsAndCorrectTotal()
    {
        var checkIn = new DateOnly(2026, 6, 1);
        var checkOut = new DateOnly(2026, 6, 5); // 4 nights: Jun 1, 2, 3, 4
        const decimal baseRate = 100m;
        var seasonal = new SeasonalRate(
            new DateOnly(2026, 6, 3),
            new DateOnly(2026, 6, 5), // covers Jun 3, 4
            150m,
            "Summer Weekend");

        var result = NightlyRateCalculator.Calculate(checkIn, checkOut, baseRate, [seasonal]);

        result.LineItems.Should().HaveCount(4);

        var baseNights = result.LineItems.Where(li => li.RateType == RateType.BaseRate).ToList();
        var seasonalNights = result.LineItems.Where(li => li.RateType == RateType.SeasonalRate).ToList();

        baseNights.Should().HaveCount(2);
        baseNights.Should().AllSatisfy(li => li.Rate.Should().Be(100m));

        seasonalNights.Should().HaveCount(2);
        seasonalNights.Should().AllSatisfy(li =>
        {
            li.Rate.Should().Be(150m);
            li.SeasonalRateName.Should().Be("Summer Weekend");
        });

        result.TotalPrice.Should().Be(200m + 300m); // 2×100 + 2×150
    }

    // AC-3: Seasonal Rate covers the full stay
    [Fact]
    public void Calculate_FullSeasonalRate_ReturnsSingleGroupAndCorrectTotal()
    {
        var checkIn = new DateOnly(2026, 7, 1);
        var checkOut = new DateOnly(2026, 7, 4); // 3 nights
        const decimal baseRate = 100m;
        var seasonal = new SeasonalRate(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31),
            200m,
            "Peak Summer");

        var result = NightlyRateCalculator.Calculate(checkIn, checkOut, baseRate, [seasonal]);

        result.LineItems.Should().HaveCount(3);
        result.LineItems.Should().AllSatisfy(li =>
        {
            li.Rate.Should().Be(200m);
            li.RateType.Should().Be(RateType.SeasonalRate);
            li.SeasonalRateName.Should().Be("Peak Summer");
        });
        result.TotalPrice.Should().Be(600m);
    }

    // AC-4: Overlapping Seasonal Rates — most specific (narrowest) wins
    [Fact]
    public void Calculate_OverlappingSeasonalRates_NarrowestRangeWins()
    {
        var checkIn = new DateOnly(2026, 7, 10);
        var checkOut = new DateOnly(2026, 7, 11); // 1 night: Jul 10
        const decimal baseRate = 100m;

        var broad = new SeasonalRate(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31), // 30-day range
            150m,
            "July Broad");

        var narrow = new SeasonalRate(
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 12), // 2-day range
            120m,
            "July Narrow");

        var result = NightlyRateCalculator.Calculate(checkIn, checkOut, baseRate, [broad, narrow]);

        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].Rate.Should().Be(120m);
        result.LineItems[0].SeasonalRateName.Should().Be("July Narrow");
    }

    // AC-5: Overlapping Seasonal Rates — equal specificity → higher rate wins (EC-005)
    [Fact]
    public void Calculate_EqualSpecificitySeasonalRates_HigherRateWins()
    {
        var checkIn = new DateOnly(2026, 8, 1);
        var checkOut = new DateOnly(2026, 8, 2); // 1 night: Aug 1
        const decimal baseRate = 100m;

        var rateA = new SeasonalRate(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 8), // 7-day range
            180m,
            "Rate A");

        var rateB = new SeasonalRate(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 8), // identical 7-day range
            220m,
            "Rate B");

        var result = NightlyRateCalculator.Calculate(checkIn, checkOut, baseRate, [rateA, rateB]);

        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].Rate.Should().Be(220m);
        result.LineItems[0].SeasonalRateName.Should().Be("Rate B");
    }

    // AC-6: Zero-night range rejected (EC-010)
    [Fact]
    public void Calculate_SameDayCheckInAndCheckOut_ThrowsDomainValidationException()
    {
        var date = new DateOnly(2026, 6, 1);

        var act = () => NightlyRateCalculator.Calculate(date, date, 100m, []);

        act.Should().Throw<DomainValidationException>()
            .WithMessage("*Minimum stay is 1 night*");
    }

    // AC-6 extension: check-out before check-in also rejected
    [Fact]
    public void Calculate_CheckOutBeforeCheckIn_ThrowsDomainValidationException()
    {
        var act = () => NightlyRateCalculator.Calculate(
            new DateOnly(2026, 6, 5),
            new DateOnly(2026, 6, 1),
            100m, []);

        act.Should().Throw<DomainValidationException>();
    }
}
