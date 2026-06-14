using System.Security.Claims;
using CabinConnect.Api.Models;
using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Holds;
using CabinConnect.Domain.Rates;
using CabinConnect.Domain.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CabinConnect.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public sealed class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;

    public BookingsController(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // AC-1: authenticated Guest confirms a Booking from an Active Hold
    [HttpPost]
    public async Task<IActionResult> ConfirmBooking([FromBody] ConfirmBookingRequest request, CancellationToken ct)
    {
        var guestIdStr = User.FindFirst("sub")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (guestIdStr is null || !Guid.TryParse(guestIdStr, out var guestId))
            return Unauthorized(ApiResponse<object>.Failure(
                new ApiError("UNAUTHORIZED", "Valid authentication is required.")));

        if (!Guid.TryParse(request.HoldId, out var holdId))
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_PARAMETERS", "holdId must be a valid UUID.")));

        try
        {
            var (booking, breakdown) = await _bookingService.ConfirmHoldAsync(holdId, guestId, ct);

            return Created($"/api/bookings/{booking.Id}", ApiResponse<object>.Success(new
            {
                bookingId      = booking.Id,
                holdId         = booking.HoldId,
                cabinId        = booking.CabinId,
                checkIn        = booking.CheckIn.ToString("yyyy-MM-dd"),
                checkOut       = booking.CheckOut.ToString("yyyy-MM-dd"),
                totalPrice     = booking.TotalPrice,
                currency       = booking.Currency,
                status         = booking.Status.ToString(),
                priceBreakdown = GroupBreakdown(breakdown.LineItems)
            }));
        }
        catch (HoldNotFoundException)
        {
            // AC-8: holdId does not exist
            return NotFound(ApiResponse<object>.Failure(
                new ApiError("NOT_FOUND", "Hold not found.")));
        }
        catch (HoldOwnershipException)
        {
            // AC-4: Hold belongs to a different Guest (EC-007)
            return StatusCode(403, ApiResponse<object>.Failure(
                new ApiError("FORBIDDEN", "You do not own this hold.")));
        }
        catch (HoldNotActiveException)
        {
            // AC-3: Hold is Consumed or Cancelled
            return Conflict(ApiResponse<object>.Failure(
                new ApiError("HOLD_NOT_ACTIVE", "The hold is no longer active.")));
        }
        catch (HoldExpiredException)
        {
            // AC-2: Hold's expiresAt has passed (EC-002)
            return Conflict(ApiResponse<object>.Failure(
                new ApiError("HOLD_EXPIRED", "The hold has expired. Please search again.")));
        }
    }

    private static List<object> GroupBreakdown(IReadOnlyList<RateLineItem> lineItems)
    {
        var groups = new List<object>();
        int i = 0;
        while (i < lineItems.Count)
        {
            var current = lineItems[i];
            int count = 1;
            while (i + count < lineItems.Count
                   && lineItems[i + count].Rate             == current.Rate
                   && lineItems[i + count].RateType         == current.RateType
                   && lineItems[i + count].SeasonalRateName == current.SeasonalRateName)
                count++;

            var rateLabel = current.RateType == RateType.BaseRate
                ? "Base Rate"
                : current.SeasonalRateName ?? "Seasonal Rate";
            var nightsWord = count == 1 ? "night" : "nights";

            groups.Add(new
            {
                label        = $"{count} {nightsWord} × {current.Rate:N2} ({rateLabel})",
                nights       = count,
                ratePerNight = current.Rate,
                rateType     = current.RateType == RateType.BaseRate ? "BaseRate" : "SeasonalRate",
                subtotal     = current.Rate * count
            });

            i += count;
        }
        return groups;
    }
}
