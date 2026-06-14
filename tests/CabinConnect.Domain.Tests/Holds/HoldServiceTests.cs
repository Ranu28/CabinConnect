using CabinConnect.Domain.Holds;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CabinConnect.Domain.Tests.Holds;

public class HoldServiceTests
{
    private static readonly Guid      CabinId  = Guid.NewGuid();
    private static readonly Guid      GuestId  = Guid.NewGuid();
    private static readonly DateOnly  CheckIn  = new(2026, 8, 1);
    private static readonly DateOnly  CheckOut = new(2026, 8, 5);

    private static (HoldService Service, IHoldRepository Repo) Build()
    {
        var repo = Substitute.For<IHoldRepository>();
        return (new HoldService(repo), repo);
    }

    private static Hold MakeHold() => new()
    {
        Id        = Guid.NewGuid(),
        CabinId   = CabinId,
        GuestId   = GuestId,
        CheckIn   = CheckIn,
        CheckOut  = CheckOut,
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
        Status    = HoldStatus.Active
    };

    // AC-1: delegates to repository and returns the Hold
    [Fact]
    public async Task PlaceHoldAsync_ValidInput_ReturnsHoldFromRepository()
    {
        var (service, repo) = Build();
        var expected = MakeHold();
        repo.PlaceHoldAsync(CabinId, GuestId, CheckIn, CheckOut, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await service.PlaceHoldAsync(CabinId, GuestId, CheckIn, CheckOut);

        result.Should().BeSameAs(expected);
        await repo.Received(1).PlaceHoldAsync(CabinId, GuestId, CheckIn, CheckOut, Arg.Any<CancellationToken>());
    }

    // AC-2/AC-3/AC-4: CabinUnavailableException propagates
    [Fact]
    public async Task PlaceHoldAsync_CabinUnavailable_ThrowsCabinUnavailableException()
    {
        var (service, repo) = Build();
        repo.PlaceHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new CabinUnavailableException());

        await service.Invoking(s => s.PlaceHoldAsync(CabinId, GuestId, CheckIn, CheckOut))
            .Should().ThrowAsync<CabinUnavailableException>();
    }

    // AC-9: CabinNotFoundException propagates
    [Fact]
    public async Task PlaceHoldAsync_CabinNotFound_ThrowsCabinNotFoundException()
    {
        var (service, repo) = Build();
        repo.PlaceHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new CabinNotFoundException(CabinId));

        await service.Invoking(s => s.PlaceHoldAsync(CabinId, GuestId, CheckIn, CheckOut))
            .Should().ThrowAsync<CabinNotFoundException>();
    }
}
