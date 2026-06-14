using System.Security.Claims;
using CabinConnect.Api.Models;
using CabinConnect.Domain.Holds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CabinConnect.Api.Controllers;

[ApiController]
[Route("api/holds")]
[Authorize]
public sealed class HoldsController : ControllerBase
{
    private readonly HoldService _holdService;

    public HoldsController(HoldService holdService)
    {
        _holdService = holdService;
    }

    // AC-1: authenticated Guest places a Hold on an available Cabin
    [HttpPost]
    public async Task<IActionResult> PlaceHold([FromBody] PlaceHoldRequest request, CancellationToken ct)
    {
        // Extract guest ID from the JWT "sub" claim (Supabase standard).
        var guestIdStr = User.FindFirst("sub")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (guestIdStr is null || !Guid.TryParse(guestIdStr, out var guestId))
            return Unauthorized(ApiResponse<object>.Failure(
                new ApiError("UNAUTHORIZED", "Valid authentication is required.")));

        // AC-7: check-out ≤ check-in (EC-009)
        if (!DateOnly.TryParseExact(request.CheckIn,  "yyyy-MM-dd", out var checkIn)
         || !DateOnly.TryParseExact(request.CheckOut, "yyyy-MM-dd", out var checkOut))
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_PARAMETERS", "checkIn and checkOut must be valid ISO 8601 dates (yyyy-MM-dd).")));

        // AC-8: zero-night stay (EC-010) — evaluated before less-than to emit the correct code
        if (checkIn == checkOut)
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("ZERO_NIGHT_STAY", "Minimum stay is 1 night.")));

        // AC-7: check-out before check-in
        if (checkOut < checkIn)
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_DATE_RANGE", "Check-out date must be after check-in date.")));

        if (!Guid.TryParse(request.CabinId, out var cabinId))
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_PARAMETERS", "cabinId must be a valid UUID.")));

        try
        {
            var hold = await _holdService.PlaceHoldAsync(cabinId, guestId, checkIn, checkOut, ct);

            return Created($"/api/holds/{hold.Id}", ApiResponse<object>.Success(new
            {
                holdId    = hold.Id,
                cabinId   = hold.CabinId,
                checkIn   = hold.CheckIn.ToString("yyyy-MM-dd"),
                checkOut  = hold.CheckOut.ToString("yyyy-MM-dd"),
                expiresAt = hold.ExpiresAt,
                status    = hold.Status.ToString()
            }));
        }
        catch (CabinNotFoundException)
        {
            // AC-9: cabin does not exist or is unpublished → 404
            return NotFound(ApiResponse<object>.Failure(
                new ApiError("NOT_FOUND", "Cabin not found or not available for booking.")));
        }
        catch (CabinUnavailableException)
        {
            // AC-2, AC-3, AC-4: overlapping booking, hold, or blackout date
            return Conflict(ApiResponse<object>.Failure(
                new ApiError("CABIN_UNAVAILABLE", "The cabin is not available for the requested dates.")));
        }
    }
}
