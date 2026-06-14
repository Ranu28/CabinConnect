using System.Net;
using System.Net.Http.Json;
using CabinConnect.Api.Tests.Auth;
using CabinConnect.Domain.Holds;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CabinConnect.Api.Tests.Controllers;

public class HoldsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HoldsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient BuildClient(IHoldRepository? repo = null)
    {
        repo ??= Substitute.For<IHoldRepository>();

        return _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                // Replace the real auth scheme with FakeAuthHandler for tests.
                // Requests carrying X-Test-Auth are authenticated; others get 401.
                services.AddAuthentication(FakeAuthHandler.Scheme)
                    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(FakeAuthHandler.Scheme, _ => { });

                services.AddScoped<IHoldRepository>(_ => repo);
            }))
            .CreateClient();
    }

    private static Hold MakeHold(Guid? cabinId = null) => new()
    {
        Id        = Guid.NewGuid(),
        CabinId   = cabinId ?? Guid.NewGuid(),
        GuestId   = Guid.Parse(FakeAuthHandler.GuestId),
        CheckIn   = new DateOnly(2026, 8, 1),
        CheckOut  = new DateOnly(2026, 8, 5),
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
        Status    = HoldStatus.Active
    };

    private static HttpContent HoldBody(string cabinId = "aaaaaaaa-1111-0000-0000-000000000001",
        string checkIn = "2026-08-01", string checkOut = "2026-08-05")
        => JsonContent.Create(new { cabinId, checkIn, checkOut });

    // AC-1: valid request from authenticated guest returns 201 with holdId and expiresAt
    [Fact]
    public async Task PlaceHold_ValidRequest_Returns201WithHoldDetails()
    {
        var hold = MakeHold();
        var repo = Substitute.For<IHoldRepository>();
        repo.PlaceHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(hold);

        var client = BuildClient(repo);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/holds")
        {
            Content = HoldBody(hold.CabinId.ToString()),
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<HoldEnvelope>();
        body!.Data!.HoldId.Should().NotBeEmpty();
        body.Data.ExpiresAt.Should().NotBe(default);
        body.Data.Status.Should().Be("Active");
    }

    // AC-2/AC-3/AC-4: cabin unavailable returns 409 CABIN_UNAVAILABLE
    [Fact]
    public async Task PlaceHold_CabinUnavailable_Returns409()
    {
        var repo = Substitute.For<IHoldRepository>();
        repo.PlaceHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new CabinUnavailableException());

        var client = BuildClient(repo);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/holds")
        {
            Content = HoldBody(),
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("CABIN_UNAVAILABLE");
    }

    // AC-6: unauthenticated request returns 401
    [Fact]
    public async Task PlaceHold_NoAuth_Returns401()
    {
        var client = BuildClient();

        // No X-Test-Auth header → FakeAuthHandler returns NoResult → challenge → 401
        var response = await client.PostAsync("/api/holds", HoldBody());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC-7: checkOut before checkIn returns 400 INVALID_DATE_RANGE
    [Fact]
    public async Task PlaceHold_CheckOutBeforeCheckIn_Returns400InvalidDateRange()
    {
        var client = BuildClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/holds")
        {
            Content = HoldBody(checkIn: "2026-08-05", checkOut: "2026-08-01"),
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("INVALID_DATE_RANGE");
    }

    // AC-8: same-day check-in/check-out returns 400 ZERO_NIGHT_STAY (EC-010)
    [Fact]
    public async Task PlaceHold_SameDayCheckInAndCheckOut_Returns400ZeroNightStay()
    {
        var client = BuildClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/holds")
        {
            Content = HoldBody(checkIn: "2026-08-01", checkOut: "2026-08-01"),
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("ZERO_NIGHT_STAY");
    }

    // AC-9: cabin not found returns 404
    [Fact]
    public async Task PlaceHold_CabinNotFound_Returns404()
    {
        var repo = Substitute.For<IHoldRepository>();
        repo.PlaceHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new CabinNotFoundException(Guid.NewGuid()));

        var client = BuildClient(repo);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/holds")
        {
            Content = HoldBody(),
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("NOT_FOUND");
    }

    // Helpers for deserialising responses
    private sealed record HoldEnvelope(HoldData? Data, object? Error);
    private sealed record HoldData(Guid HoldId, Guid CabinId, string CheckIn, string CheckOut, DateTimeOffset ExpiresAt, string Status);
    private sealed record ErrorEnvelope(object? Data, ErrorBody? Error);
    private sealed record ErrorBody(string Code, string Message);
}
