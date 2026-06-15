using System.Net;
using System.Net.Http.Json;
using CabinConnect.Api.Tests.Auth;
using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Common;
using CabinConnect.Domain.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CabinConnect.Api.Tests.Controllers;

public class HostControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HostControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient BuildClient(IBookingRepository? repo = null)
    {
        repo ??= Substitute.For<IBookingRepository>();

        var profileRepo = Substitute.For<IUserProfileRepository>();
        profileRepo.GetByIdAsync(Guid.Parse(FakeAuthHandler.GuestId), Arg.Any<CancellationToken>())
            .Returns(new UserProfile(Guid.Parse(FakeAuthHandler.GuestId), "host", "Test Host"));

        return _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                services.AddAuthentication(FakeAuthHandler.Scheme)
                    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(FakeAuthHandler.Scheme, _ => { });
                services.AddScoped<IBookingRepository>(_ => repo);
                services.AddScoped<IUserProfileRepository>(_ => profileRepo);
            }))
            .CreateClient();
    }

    private static PagedResult<HostBookingItem> EmptyPage() =>
        new([], 1, 10, 0);

    private static PagedResult<HostBookingItem> OneItemPage() =>
        new([new HostBookingItem(
            Guid.NewGuid(), Guid.NewGuid(), "Pine Cabin",
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5),
            400m, "USD", BookingStatus.Confirmed)], 1, 10, 1);

    // AC-1: authenticated Host receives paginated bookings
    [Fact]
    public async Task GetHostBookings_Authenticated_Returns200WithItems()
    {
        var repo = Substitute.For<IBookingRepository>();
        repo.GetHostBookingsAsync(Arg.Any<Guid>(), null, 1, 10, Arg.Any<CancellationToken>())
            .Returns(OneItemPage());

        var client  = BuildClient(repo);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/host/bookings")
        {
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Envelope>();
        body!.Data!.TotalCount.Should().Be(1);
        body.Data.Items.Should().HaveCount(1);
        body.Data.Items[0].CabinName.Should().Be("Pine Cabin");
        body.Data.Items[0].Status.Should().Be("Confirmed");
    }

    // AC-2: status filter forwarded to repository
    [Fact]
    public async Task GetHostBookings_StatusFilter_PassesFilterToRepository()
    {
        var repo = Substitute.For<IBookingRepository>();
        repo.GetHostBookingsAsync(Arg.Any<Guid>(), BookingStatus.Confirmed, 1, 10, Arg.Any<CancellationToken>())
            .Returns(EmptyPage());

        var client  = BuildClient(repo);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/host/bookings?status=Confirmed")
        {
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        await client.SendAsync(request);

        await repo.Received(1).GetHostBookingsAsync(
            Arg.Any<Guid>(), BookingStatus.Confirmed, 1, 10, Arg.Any<CancellationToken>());
    }

    // AC-2: invalid status value returns 400
    [Fact]
    public async Task GetHostBookings_InvalidStatus_Returns400()
    {
        var client  = BuildClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/host/bookings?status=Bogus")
        {
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // AC-4: unauthenticated request returns 401
    [Fact]
    public async Task GetHostBookings_NoAuth_Returns401()
    {
        var response = await BuildClient().GetAsync("/api/host/bookings");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Helpers
    private sealed record Envelope(HostBookingListData? Data, object? Error);
    private sealed record HostBookingListData(HostBookingDto[] Items, int Page, int PageSize, int TotalCount);
    private sealed record HostBookingDto(string BookingId, string CabinName, string GuestRef, string Status);
}
