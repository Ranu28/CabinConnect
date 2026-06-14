using CabinConnect.Domain.Cabins;
using CabinConnect.Domain.Rates;
using CabinConnect.Domain.Search;
using FluentAssertions;
using NSubstitute;

namespace CabinConnect.Domain.Tests.Search;

// Availability filtering (AC-1, AC-2, AC-4, AC-5, AC-11), sorting (AC-6), and
// pagination (AC-12, AC-13) are now enforced by the SQL in CabinSearchRepository.
// Service tests cover what the service itself does: price calculation (AC-3),
// breakdown construction, and result mapping from the repository page.

public class CabinSearchServiceTests
{
    private static readonly DateOnly CheckIn  = new(2026, 7, 1);
    private static readonly DateOnly CheckOut = new(2026, 7, 5); // 4 nights

    private static Cabin MakeCabin(
        decimal baseRate = 100m,
        int maxGuests = 4,
        string[]? amenities = null,
        IReadOnlyList<SeasonalRate>? seasonalRates = null) => new()
    {
        Id            = Guid.NewGuid(),
        Name          = "Test Cabin",
        Description   = "A cabin.",
        MaxGuests     = maxGuests,
        BaseRate      = baseRate,
        Currency      = "USD",
        Amenities     = amenities ?? ["wifi", "parking"],
        SeasonalRates = seasonalRates ?? [],
        IsPublished   = true
    };

    private static CabinSearchService BuildService(
        IReadOnlyList<Cabin> page, int totalCount = -1)
    {
        if (totalCount < 0) totalCount = page.Count;
        var repo = Substitute.For<ICabinSearchRepository>();
        repo.SearchAvailablePageAsync(Arg.Any<CabinSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns((page, totalCount));
        return new CabinSearchService(repo);
    }

    private static CabinSearchQuery DefaultQuery() => new(CheckIn, CheckOut);

    // AC-3: available cabin appears with correct price breakdown
    [Fact]
    public async Task SearchAsync_AvailableCabin_ReturnsWithPriceBreakdown()
    {
        var cabin = MakeCabin(baseRate: 100m);
        var result = await BuildService([cabin]).SearchAsync(DefaultQuery());

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.TotalPrice.Should().Be(400m);                        // 4 nights × $100
        item.PriceBreakdown.Should().HaveCount(1);
        item.PriceBreakdown[0].RateType.Should().Be("BaseRate");
        item.PriceBreakdown[0].Nights.Should().Be(4);
        item.PriceBreakdown[0].Subtotal.Should().Be(400m);
    }

    // AC-3 with seasonal rate: breakdown groups consecutive nights at same rate
    [Fact]
    public async Task SearchAsync_CabinWithSeasonalRate_ReturnsCorrectBreakdown()
    {
        // Seasonal rate covers nights 1–2 (Jul 1–2); nights 3–4 fall back to base rate
        var seasonal = new SeasonalRate(
            StartDate: new DateOnly(2026, 7, 1),
            EndDate:   new DateOnly(2026, 7, 3),  // exclusive: covers Jul 1 and Jul 2
            Rate:      200m,
            Name:      "Peak");
        var cabin = MakeCabin(baseRate: 100m, seasonalRates: [seasonal]);

        var result = await BuildService([cabin]).SearchAsync(DefaultQuery());

        var item = result.Items[0];
        item.TotalPrice.Should().Be(600m); // 2 × $200 + 2 × $100
        item.PriceBreakdown.Should().HaveCount(2);
        item.PriceBreakdown[0].RateType.Should().Be("SeasonalRate");
        item.PriceBreakdown[0].Nights.Should().Be(2);
        item.PriceBreakdown[0].Subtotal.Should().Be(400m);
        item.PriceBreakdown[1].RateType.Should().Be("BaseRate");
        item.PriceBreakdown[1].Nights.Should().Be(2);
        item.PriceBreakdown[1].Subtotal.Should().Be(200m);
    }

    // Empty repository response → empty paged result
    [Fact]
    public async Task SearchAsync_NoResults_ReturnsEmptyPagedResult()
    {
        var result = await BuildService([]).SearchAsync(DefaultQuery());

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // Service passes TotalCount and pagination metadata through from the repository
    [Fact]
    public async Task SearchAsync_PassesThroughPaginationMetadata()
    {
        var cabins = Enumerable.Range(1, 2).Select(_ => MakeCabin()).ToArray();
        var query  = DefaultQuery() with { Page = 3, PageSize = 2 };
        var result = await BuildService(cabins, totalCount: 10).SearchAsync(query);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(10);
        result.Page.Should().Be(3);
        result.PageSize.Should().Be(2);
    }

    // Service preserves the order returned by the repository (SQL sorts by total_price)
    [Fact]
    public async Task SearchAsync_PreservesRepositoryOrder()
    {
        var cabins = new[] { MakeCabin(100m), MakeCabin(200m), MakeCabin(300m) };
        var result = await BuildService(cabins).SearchAsync(DefaultQuery());

        result.Items.Select(r => r.TotalPrice)
            .Should().BeInAscendingOrder();
    }

    // Result fields are mapped correctly from the Cabin domain object
    [Fact]
    public async Task SearchAsync_MapsAllResultFields()
    {
        var cabin = new Cabin
        {
            Id            = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
            Name          = "Pine Cabin",
            Description   = "Nice",
            ImageUrl      = "https://example.com/img.jpg",
            MaxGuests     = 6,
            BaseRate      = 150m,
            Currency      = "USD",
            Amenities     = ["wifi", "fireplace"],
            SeasonalRates = [],
            Location      = new CabinLocation(48.5, -123.2),
            IsPublished   = true
        };

        var result = await BuildService([cabin]).SearchAsync(DefaultQuery());

        var item = result.Items[0];
        item.CabinId.Should().Be(cabin.Id);
        item.Name.Should().Be("Pine Cabin");
        item.MaxGuests.Should().Be(6);
        item.Amenities.Should().Contain("wifi").And.Contain("fireplace");
        item.Currency.Should().Be("USD");
        item.Location.Should().NotBeNull();
        item.Location!.Lat.Should().Be(48.5);
    }
}
