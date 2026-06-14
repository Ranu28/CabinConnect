using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Holds;
using CabinConnect.Domain.Rates;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CabinConnect.Domain.Tests.Bookings;

public class BookingServiceTests
{
    private static readonly Guid HoldId  = Guid.NewGuid();
    private static readonly Guid GuestId = Guid.NewGuid();

    private static (BookingService Service, IBookingRepository Repo) Build()
    {
        var repo = Substitute.For<IBookingRepository>();
        return (new BookingService(repo), repo);
    }

    private static (Booking, RateBreakdown) MakeResult() =>
    (
        new Booking
        {
            Id         = Guid.NewGuid(),
            CabinId    = Guid.NewGuid(),
            GuestId    = GuestId,
            HoldId     = HoldId,
            CheckIn    = new DateOnly(2026, 8, 1),
            CheckOut   = new DateOnly(2026, 8, 5),
            Status     = BookingStatus.Confirmed,
            TotalPrice = 400m,
            Currency   = "USD"
        },
        new RateBreakdown([new RateLineItem(new DateOnly(2026, 8, 1), 100m, RateType.BaseRate, null)])
    );

    // AC-1: delegates to repository and returns (Booking, RateBreakdown)
    [Fact]
    public async Task ConfirmHoldAsync_ValidHold_ReturnsTupleFromRepository()
    {
        var (service, repo) = Build();
        var expected = MakeResult();
        repo.ConfirmHoldAsync(HoldId, GuestId, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await service.ConfirmHoldAsync(HoldId, GuestId);

        result.Booking.Should().BeSameAs(expected.Item1);
        result.PriceBreakdown.Should().BeSameAs(expected.Item2);
        await repo.Received(1).ConfirmHoldAsync(HoldId, GuestId, Arg.Any<CancellationToken>());
    }

    // AC-2: HoldExpiredException propagates
    [Fact]
    public async Task ConfirmHoldAsync_ExpiredHold_ThrowsHoldExpiredException()
    {
        var (service, repo) = Build();
        repo.ConfirmHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HoldExpiredException());

        await service.Invoking(s => s.ConfirmHoldAsync(HoldId, GuestId))
            .Should().ThrowAsync<HoldExpiredException>();
    }

    // AC-3: HoldNotActiveException propagates
    [Fact]
    public async Task ConfirmHoldAsync_ConsumedHold_ThrowsHoldNotActiveException()
    {
        var (service, repo) = Build();
        repo.ConfirmHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HoldNotActiveException());

        await service.Invoking(s => s.ConfirmHoldAsync(HoldId, GuestId))
            .Should().ThrowAsync<HoldNotActiveException>();
    }

    // AC-4: HoldOwnershipException propagates
    [Fact]
    public async Task ConfirmHoldAsync_WrongGuest_ThrowsHoldOwnershipException()
    {
        var (service, repo) = Build();
        repo.ConfirmHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HoldOwnershipException());

        await service.Invoking(s => s.ConfirmHoldAsync(HoldId, GuestId))
            .Should().ThrowAsync<HoldOwnershipException>();
    }

    // AC-8: HoldNotFoundException propagates
    [Fact]
    public async Task ConfirmHoldAsync_HoldNotFound_ThrowsHoldNotFoundException()
    {
        var (service, repo) = Build();
        repo.ConfirmHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HoldNotFoundException(HoldId));

        await service.Invoking(s => s.ConfirmHoldAsync(HoldId, GuestId))
            .Should().ThrowAsync<HoldNotFoundException>();
    }
}
