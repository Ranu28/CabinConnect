using System.Net;
using System.Net.Http.Json;
using CabinConnect.Domain.Search;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CabinConnect.Api.Tests.Controllers;

public class CabinsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CabinsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient BuildClient(ICabinSearchRepository? repo = null)
    {
        repo ??= Substitute.For<ICabinSearchRepository>();
        repo.GetPublishedCabinsWithAvailabilityDataAsync(
                Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);

        return _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                services.AddScoped<ICabinSearchRepository>(_ => repo);
            })).CreateClient();
    }

    // AC-7: checkOut before checkIn returns 400 INVALID_DATE_RANGE
    [Fact]
    public async Task Search_CheckOutBeforeCheckIn_Returns400InvalidDateRange()
    {
        var client = BuildClient();

        var response = await client.GetAsync(
            "/api/cabins/search?checkIn=2026-07-10&checkOut=2026-07-05");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("INVALID_DATE_RANGE");
    }

    // AC-8: checkIn equals checkOut returns 400 ZERO_NIGHT_STAY (EC-010)
    [Fact]
    public async Task Search_SameDayCheckInAndCheckOut_Returns400ZeroNightStay()
    {
        var client = BuildClient();

        var response = await client.GetAsync(
            "/api/cabins/search?checkIn=2026-07-10&checkOut=2026-07-10");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("ZERO_NIGHT_STAY");
    }

    // AC-10: public endpoint — no auth required returns 200
    [Fact]
    public async Task Search_ValidRequestWithNoAuth_Returns200()
    {
        var client = BuildClient();

        var response = await client.GetAsync(
            "/api/cabins/search?checkIn=2026-07-01&checkOut=2026-07-05");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AC-14: pageSize > 100 returns 400 INVALID_PARAMETERS
    [Fact]
    public async Task Search_PageSizeExceedsMax_Returns400InvalidParameters()
    {
        var client = BuildClient();

        var response = await client.GetAsync(
            "/api/cabins/search?checkIn=2026-07-01&checkOut=2026-07-05&pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("INVALID_PARAMETERS");
    }

    // AC-14: page < 1 returns 400 INVALID_PARAMETERS
    [Fact]
    public async Task Search_PageLessThanOne_Returns400InvalidParameters()
    {
        var client = BuildClient();

        var response = await client.GetAsync(
            "/api/cabins/search?checkIn=2026-07-01&checkOut=2026-07-05&page=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("INVALID_PARAMETERS");
    }

    // AC-14: unparseable date returns 400 INVALID_PARAMETERS
    [Fact]
    public async Task Search_InvalidDateFormat_Returns400InvalidParameters()
    {
        var client = BuildClient();

        var response = await client.GetAsync(
            "/api/cabins/search?checkIn=not-a-date&checkOut=2026-07-05");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error!.Code.Should().Be("INVALID_PARAMETERS");
    }

    // Helpers for deserialising the error envelope
    private sealed record ErrorEnvelope(object? Data, ErrorBody? Error);
    private sealed record ErrorBody(string Code, string Message);
}
