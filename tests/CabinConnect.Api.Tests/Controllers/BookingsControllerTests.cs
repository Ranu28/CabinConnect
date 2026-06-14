using System.Net;
using System.Net.Http.Json;
using CabinConnect.Api.Tests.Auth;
using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Holds;
using CabinConnect.Domain.Rates;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CabinConnect.Api.Tests.Controllers;

public class BookingsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BookingsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient BuildClient(IBookingRepository? repo = null)
    {
        repo ??= Substitute.For<IBookingRepository>();

        return _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                services.AddAuthentication(FakeAuthHandler.Scheme)
                    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(FakeAuthHandler.Scheme, _ => { });

                services.AddScoped<IBookingRepository>(_ => repo);
            }))
            .CreateClient();
    }

    private static (Booking, RateBreakdown) MakeConfirmedResult()
    {
        var booking = new Booking
        {
            Id         = Guid.NewGuid(),
            CabinId    = Guid.NewGuid(),
            GuestId    = Guid.Parse(FakeAuthHandler.GuestId),
            HoldId     = Guid.NewGuid(),
            CheckIn    = new DateOnly(2026, 8, 1),
            CheckOut   = new DateOnly(2026, 8, 5),
            Status     = BookingStatus.Confirmed,
            TotalPrice = 400m,
            Currency   = "USD"
        };
        var breakdown = new RateBreakdown(
        [
            new RateLineItem(new DateOnly(2026, 8, 1), 100m, RateType.BaseRate, null),
            new RateLineItem(new DateOnly(2026, 8, 2), 100m, RateType.BaseRate, null),
            new RateLineItem(new DateOnly(2026, 8, 3), 100m, RateType.BaseRate, null),
            new RateLineItem(new DateOnly(2026, 8, 4), 100m, RateType.BaseRate, null),
        ]);
        return (booking, breakdown);
    }

    private static HttpContent Body(string holdId = "bbbbbbbb-0000-0000-0000-000000000001")
        => JsonContent.Create(new { holdId });

    // AC-1: valid confirm returns 201 with bookingId, totalPrice, status=Confirmed
    [Fact]
    public async Task ConfirmBooking_ValidHold_Returns201WithBookingDetails()
    {
        var (booking, breakdown) = MakeConfirmedResult();
        var repo = Substitute.For<IBookingRepository>();
        repo.ConfirmHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((booking, breakdown));

        var client = BuildClient(repo);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = Body(booking.HoldId.ToString()!),
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ConfirmEnvelope>();
        body!.Data!.BookingId.Should().NotBeEmpty();
        body.Data.TotalPrice.Should().Be(400m);
        body.Data.Status.Should().Be("Confirmed");
        body.Data.Currency.Should().Be("USD");
    }

    // AC-2: expired Hold returns 409 HOLD_EXPIRED (EC-002)
    [Fact]
    public async Task ConfirmBooking_ExpiredHold_Returns409HoldExpired()
    {
        var repo = Substitute.For<IBookingRepository>();
        repo.ConfirmHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HoldExpiredException());

        var client = BuildClient(repo);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = Body(),
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("HOLD_EXPIRED");
    }

    // AC-3: Consumed/Cancelled Hold returns 409 HOLD_NOT_ACTIVE
    [Fact]
    public async Task ConfirmBooking_InactiveHold_Returns409HoldNotActive()
    {
        var repo = Substitute.For<IBookingRepository>();
        repo.ConfirmHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HoldNotActiveException());

        var client = BuildClient(repo);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = Body(),
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("HOLD_NOT_ACTIVE");
    }

    // AC-4: Hold owned by different Guest returns 403 (EC-007)
    [Fact]
    public async Task ConfirmBooking_WrongGuest_Returns403()
    {
        var repo = Substitute.For<IBookingRepository>();
        repo.ConfirmHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HoldOwnershipException());

        var client = BuildClient(repo);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = Body(),
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // AC-5: unauthenticated request returns 401
    [Fact]
    public async Task ConfirmBooking_NoAuth_Returns401()
    {
        var client = BuildClient();

        var response = await client.PostAsync("/api/bookings", Body());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // AC-8: Hold not found returns 404
    [Fact]
    public async Task ConfirmBooking_HoldNotFound_Returns404()
    {
        var repo = Substitute.For<IBookingRepository>();
        repo.ConfirmHoldAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HoldNotFoundException(Guid.NewGuid()));

        var client = BuildClient(repo);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = Body(),
            Headers = { { FakeAuthHandler.AuthHeader, "true" } }
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("NOT_FOUND");
    }

    // Helpers for deserialising responses
    private sealed record ConfirmEnvelope(BookingData? Data, object? Error);
    private sealed record BookingData(Guid BookingId, Guid CabinId, string CheckIn, string CheckOut,
        decimal TotalPrice, string Currency, string Status);
    private sealed record ErrorEnvelope(object? Data, ErrorBody? Error);
    private sealed record ErrorBody(string Code, string Message);
}
