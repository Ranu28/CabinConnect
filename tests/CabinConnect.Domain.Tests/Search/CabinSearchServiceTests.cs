using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Cabins;
using CabinConnect.Domain.Rates;
using CabinConnect.Domain.Search;
using FluentAssertions;
using NSubstitute;

namespace CabinConnect.Domain.Tests.Search;

public class CabinSearchServiceTests
{
    private static readonly DateOnly CheckIn = new(2026, 7, 1);
    private static readonly DateOnly CheckOut = new(2026, 7, 5); // 4 nights

    private static Cabin MakeCabin(
        decimal baseRate = 100m,
        int maxGuests = 4,
        string[]? amenities = null,
        bool isPublished = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Cabin",
        Description = "A cabin.",
        MaxGuests = maxGuests,
        BaseRate = baseRate,
        Currency = "USD",
        Amenities = amenities ?? ["wifi", "parking"],
        SeasonalRates = [],
        IsPublished = isPublished
    };

    private static Booking MakeBooking(Guid cabinId, BookingStatus status) => new()
    {
        Id = Guid.NewGuid(),
        CabinId = cabinId,
        CheckIn = CheckIn,
        CheckOut = CheckOut,
        Status = status
    };

    private static BlackoutDate MakeBlackout(Guid cabinId) => new()
    {
        Id = Guid.NewGuid(),
        CabinId = cabinId,
        StartDate = CheckIn,
        EndDate = CheckOut
    };

    private static CabinSearchService BuildService(
        IReadOnlyList<CabinWithAvailabilityData> repoData)
    {
        var repo = Substitute.For<ICabinSearchRepository>();
        repo.GetPublishedCabinsWithAvailabilityDataAsync(
                Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(repoData);
        return new CabinSearchService(repo);
    }

    private static CabinSearchQuery DefaultQuery() =>
        new(CheckIn, CheckOut);

    // AC-1: confirmed booking blocks cabin
    [Fact]
    public async Task SearchAsync_CabinHasConfirmedBooking_ExcludesCabin()
    {
        var cabin = MakeCabin();
        var data = new[]
        {
            new CabinWithAvailabilityData
            {
                Cabin = cabin,
                OverlappingBookings = [MakeBooking(cabin.Id, BookingStatus.Confirmed)],
                OverlappingBlackouts = []
            }
        };

        var result = await BuildService(data).SearchAsync(DefaultQuery());

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // AC-1: pending booking also blocks cabin
    [Fact]
    public async Task SearchAsync_CabinHasPendingBooking_ExcludesCabin()
    {
        var cabin = MakeCabin();
        var data = new[]
        {
            new CabinWithAvailabilityData
            {
                Cabin = cabin,
                OverlappingBookings = [MakeBooking(cabin.Id, BookingStatus.Pending)],
                OverlappingBlackouts = []
            }
        };

        var result = await BuildService(data).SearchAsync(DefaultQuery());

        result.Items.Should().BeEmpty();
    }

    // AC-1: cancelled booking does NOT block cabin
    [Fact]
    public async Task SearchAsync_CabinHasOnlyCancelledBooking_IncludesCabin()
    {
        var cabin = MakeCabin();
        var data = new[]
        {
            new CabinWithAvailabilityData
            {
                Cabin = cabin,
                OverlappingBookings = [MakeBooking(cabin.Id, BookingStatus.Cancelled)],
                OverlappingBlackouts = []
            }
        };

        var result = await BuildService(data).SearchAsync(DefaultQuery());

        result.Items.Should().HaveCount(1);
    }

    // AC-2: blackout date blocks cabin (EC-004)
    [Fact]
    public async Task SearchAsync_CabinHasBlackoutDate_ExcludesCabin()
    {
        var cabin = MakeCabin();
        var data = new[]
        {
            new CabinWithAvailabilityData
            {
                Cabin = cabin,
                OverlappingBookings = [],
                OverlappingBlackouts = [MakeBlackout(cabin.Id)]
            }
        };

        var result = await BuildService(data).SearchAsync(DefaultQuery());

        result.Items.Should().BeEmpty();
    }

    // AC-3: available cabin appears with price breakdown
    [Fact]
    public async Task SearchAsync_AvailableCabin_ReturnsWithPriceBreakdown()
    {
        var cabin = MakeCabin(baseRate: 100m);
        var data = new[]
        {
            new CabinWithAvailabilityData
            {
                Cabin = cabin, OverlappingBookings = [], OverlappingBlackouts = []
            }
        };

        var result = await BuildService(data).SearchAsync(DefaultQuery());

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.TotalPrice.Should().Be(400m); // 4 nights × $100
        item.PriceBreakdown.Should().HaveCount(1);
        item.PriceBreakdown[0].RateType.Should().Be("BaseRate");
        item.PriceBreakdown[0].Nights.Should().Be(4);
        item.PriceBreakdown[0].Subtotal.Should().Be(400m);
    }

    // AC-4: guest count filter
    [Fact]
    public async Task SearchAsync_GuestFilter_ExcludesCabinsBelowCapacity()
    {
        var small = MakeCabin(maxGuests: 2);
        var large = MakeCabin(maxGuests: 6);
        var data = new[]
        {
            new CabinWithAvailabilityData { Cabin = small, OverlappingBookings = [], OverlappingBlackouts = [] },
            new CabinWithAvailabilityData { Cabin = large, OverlappingBookings = [], OverlappingBlackouts = [] }
        };

        var query = DefaultQuery() with { Guests = 4 };
        var result = await BuildService(data).SearchAsync(query);

        result.Items.Should().HaveCount(1);
        result.Items[0].MaxGuests.Should().Be(6);
    }

    // AC-5: price range filter
    [Fact]
    public async Task SearchAsync_PriceRangeFilter_ExcludesCabinsOutsideRange()
    {
        var cheap = MakeCabin(baseRate: 50m);  // total = 200
        var expensive = MakeCabin(baseRate: 300m); // total = 1200
        var data = new[]
        {
            new CabinWithAvailabilityData { Cabin = cheap, OverlappingBookings = [], OverlappingBlackouts = [] },
            new CabinWithAvailabilityData { Cabin = expensive, OverlappingBookings = [], OverlappingBlackouts = [] }
        };

        var query = DefaultQuery() with { MinPrice = 100m, MaxPrice = 500m };
        var result = await BuildService(data).SearchAsync(query);

        result.Items.Should().HaveCount(1);
        result.Items[0].TotalPrice.Should().Be(200m);
    }

    // AC-6: results sorted ascending by total price
    [Fact]
    public async Task SearchAsync_MultipleAvailableCabins_SortsByTotalPriceAscending()
    {
        var cabins = new[] { MakeCabin(300m), MakeCabin(100m), MakeCabin(200m) };
        var data = cabins.Select(c => new CabinWithAvailabilityData
        {
            Cabin = c, OverlappingBookings = [], OverlappingBlackouts = []
        }).ToArray();

        var result = await BuildService(data).SearchAsync(DefaultQuery());

        result.Items.Select(r => r.TotalPrice)
            .Should().BeInAscendingOrder();
    }

    // AC-11: unpublished cabin excluded
    [Fact]
    public async Task SearchAsync_UnpublishedCabin_IsExcluded()
    {
        var cabin = MakeCabin(isPublished: false);
        var data = new[]
        {
            new CabinWithAvailabilityData
            {
                Cabin = cabin, OverlappingBookings = [], OverlappingBlackouts = []
            }
        };

        var result = await BuildService(data).SearchAsync(DefaultQuery());

        result.Items.Should().BeEmpty();
    }

    // AC-12: pagination returns correct slice
    [Fact]
    public async Task SearchAsync_Pagination_ReturnsCorrectPageSlice()
    {
        var cabins = Enumerable.Range(1, 5).Select(i => MakeCabin(baseRate: i * 10m)).ToArray();
        var data = cabins.Select(c => new CabinWithAvailabilityData
        {
            Cabin = c, OverlappingBookings = [], OverlappingBlackouts = []
        }).ToArray();

        var query = DefaultQuery() with { Page = 2, PageSize = 2 };
        var result = await BuildService(data).SearchAsync(query);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
    }

    // AC-13: page beyond last returns empty with correct totalCount
    [Fact]
    public async Task SearchAsync_PageBeyondLast_ReturnsEmptyWithCorrectTotalCount()
    {
        var cabins = Enumerable.Range(1, 3).Select(_ => MakeCabin()).ToArray();
        var data = cabins.Select(c => new CabinWithAvailabilityData
        {
            Cabin = c, OverlappingBookings = [], OverlappingBlackouts = []
        }).ToArray();

        var query = DefaultQuery() with { Page = 99, PageSize = 20 };
        var result = await BuildService(data).SearchAsync(query);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(3);
    }

    // Amenity filter — AND operation
    [Fact]
    public async Task SearchAsync_AmenityFilter_ExcludesCabinsMissingRequiredAmenity()
    {
        var withWifi = MakeCabin(amenities: ["wifi", "parking"]);
        var withoutWifi = MakeCabin(amenities: ["parking"]);
        var data = new[]
        {
            new CabinWithAvailabilityData { Cabin = withWifi, OverlappingBookings = [], OverlappingBlackouts = [] },
            new CabinWithAvailabilityData { Cabin = withoutWifi, OverlappingBookings = [], OverlappingBlackouts = [] }
        };

        var query = DefaultQuery() with { Amenities = ["wifi"] };
        var result = await BuildService(data).SearchAsync(query);

        result.Items.Should().HaveCount(1);
        result.Items[0].Amenities.Should().Contain("wifi");
    }
}
