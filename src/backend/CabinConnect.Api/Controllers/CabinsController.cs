using CabinConnect.Api.Models;
using CabinConnect.Domain.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CabinConnect.Api.Controllers;

[ApiController]
[Route("api/cabins")]
public sealed class CabinsController : ControllerBase
{
    private readonly CabinSearchService _searchService;

    public CabinsController(CabinSearchService searchService)
    {
        _searchService = searchService;
    }

    // BKF-005: public endpoint for cabin detail page
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCabin(Guid id, CancellationToken ct)
    {
        var cabin = await _searchService.GetCabinByIdAsync(id, ct);
        if (cabin is null)
            return NotFound(ApiResponse<object>.Failure(
                new ApiError("NOT_FOUND", "Cabin not found.")));

        return Ok(ApiResponse<object>.Success(new
        {
            cabinId     = cabin.Id,
            name        = cabin.Name,
            description = cabin.Description,
            imageUrl    = cabin.ImageUrl,
            maxGuests   = cabin.MaxGuests,
            amenities   = cabin.Amenities,
            baseRate    = cabin.BaseRate,
            currency    = cabin.Currency,
            location    = cabin.Location is null ? null : new
            {
                lat = cabin.Location.Lat,
                lng = cabin.Location.Lng
            }
        }));
    }

    // AC-10: public — no auth required
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] CabinSearchRequest request, CancellationToken ct)
    {
        // AC-14: validate pagination bounds
        if (request.Page < 1 || request.PageSize < 1 || request.PageSize > 100)
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_PARAMETERS",
                    "Page must be ≥ 1; PageSize must be between 1 and 100.")));

        // AC-9: parse as UTC date-only (EC-003)
        if (!DateOnly.TryParseExact(request.CheckIn, "yyyy-MM-dd", out var checkIn)
            || !DateOnly.TryParseExact(request.CheckOut, "yyyy-MM-dd", out var checkOut))
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_PARAMETERS",
                    "checkIn and checkOut must be valid ISO 8601 dates (yyyy-MM-dd).")));

        // AC-8: zero-night stay (EC-010) — check before less-than to emit correct code
        if (checkIn == checkOut)
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("ZERO_NIGHT_STAY", "Minimum stay is 1 night.")));

        // AC-7: check-out before check-in (EC-009)
        if (checkOut < checkIn)
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_DATE_RANGE",
                    "Check-out date must be after check-in date.")));

        var query = new CabinSearchQuery(
            CheckIn: checkIn,
            CheckOut: checkOut,
            Guests: request.Guests,
            Amenities: request.Amenities,
            MinPrice: request.MinPrice,
            MaxPrice: request.MaxPrice,
            Page: request.Page,
            PageSize: request.PageSize);

        var result = await _searchService.SearchAsync(query, ct);

        return Ok(ApiResponse<object>.Success(new
        {
            items = result.Items,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount
        }));
    }
}
