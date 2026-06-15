using System.Security.Claims;
using CabinConnect.Api.Models;
using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Holds;
using CabinConnect.Domain.Rates;
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

    // BKF-008: paginated list of the authenticated Guest's Bookings across all statuses
    [HttpGet]
    public async Task<IActionResult> GetMyBookings(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var guestId = ParseGuestId();
        if (guestId is null)
            return Unauthorized(ApiResponse<object>.Failure(
                new ApiError("UNAUTHORIZED", "Valid authentication is required.")));

        if (page < 1 || pageSize < 1 || pageSize > 50)
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_PARAMETERS", "page ≥ 1; pageSize between 1 and 50.")));

        var result = await _bookingService.GetGuestBookingsAsync(guestId.Value, page, pageSize, ct);

        return Ok(ApiResponse<object>.Success(new
        {
            items = result.Items.Select(b => new
            {
                bookingId  = b.BookingId,
                cabinId    = b.CabinId,
                cabinName  = b.CabinName,
                checkIn    = b.CheckIn.ToString("yyyy-MM-dd"),
                checkOut   = b.CheckOut.ToString("yyyy-MM-dd"),
                totalPrice = b.TotalPrice,
                currency   = b.Currency,
                status     = b.Status.ToString()
            }),
            page       = result.Page,
            pageSize   = result.PageSize,
            totalCount = result.TotalCount
        }));
    }

    // BKF-007: fetch a single Booking owned by this Guest (for confirmation page deep-link)
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBooking(Guid id, CancellationToken ct)
    {
        var guestId = ParseGuestId();
        if (guestId is null)
            return Unauthorized(ApiResponse<object>.Failure(
                new ApiError("UNAUTHORIZED", "Valid authentication is required.")));

        var booking = await _bookingService.GetGuestBookingByIdAsync(id, guestId.Value, ct);
        if (booking is null)
            return NotFound(ApiResponse<object>.Failure(
                new ApiError("NOT_FOUND", "Booking not found.")));

        return Ok(ApiResponse<object>.Success(new
        {
            bookingId  = booking.BookingId,
            cabinId    = booking.CabinId,
            cabinName  = booking.CabinName,
            checkIn    = booking.CheckIn.ToString("yyyy-MM-dd"),
            checkOut   = booking.CheckOut.ToString("yyyy-MM-dd"),
            totalPrice = booking.TotalPrice,
            currency   = booking.Currency,
            status     = booking.Status.ToString()
        }));
    }

    // AC-1: authenticated Guest confirms a Booking from an Active Hold
    [HttpPost]
    public async Task<IActionResult> ConfirmBooking([FromBody] ConfirmBookingRequest request, CancellationToken ct)
    {
        var guestId = ParseGuestId();
        if (guestId is null)
            return Unauthorized(ApiResponse<object>.Failure(
                new ApiError("UNAUTHORIZED", "Valid authentication is required.")));

        if (!Guid.TryParse(request.HoldId, out var holdId))
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_PARAMETERS", "holdId must be a valid UUID.")));

        try
        {
            var (booking, breakdown) = await _bookingService.ConfirmHoldAsync(holdId, guestId.Value, ct);

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
            return NotFound(ApiResponse<object>.Failure(
                new ApiError("NOT_FOUND", "Hold not found.")));
        }
        catch (HoldOwnershipException)
        {
            return StatusCode(403, ApiResponse<object>.Failure(
                new ApiError("FORBIDDEN", "You do not own this hold.")));
        }
        catch (HoldNotActiveException)
        {
            return Conflict(ApiResponse<object>.Failure(
                new ApiError("HOLD_NOT_ACTIVE", "The hold is no longer active.")));
        }
        catch (HoldExpiredException)
        {
            return Conflict(ApiResponse<object>.Failure(
                new ApiError("HOLD_EXPIRED", "The hold has expired. Please search again.")));
        }
    }

    // BKF-003: cancel a Confirmed or Pending Booking (idempotent on already-Cancelled)
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid id, CancellationToken ct)
    {
        var guestId = ParseGuestId();
        if (guestId is null)
            return Unauthorized(ApiResponse<object>.Failure(
                new ApiError("UNAUTHORIZED", "Valid authentication is required.")));

        try
        {
            await _bookingService.CancelBookingAsync(id, guestId.Value, ct);

            return Ok(ApiResponse<object>.Success(new
            {
                bookingId = id,
                status    = "Cancelled"
            }));
        }
        catch (BookingNotFoundException)
        {
            // Not found OR not owned — both return 404 to prevent info leaks (EC-007).
            return NotFound(ApiResponse<object>.Failure(
                new ApiError("NOT_FOUND", "Booking not found.")));
        }
        catch (BookingCannotBeCancelledException)
        {
            return Conflict(ApiResponse<object>.Failure(
                new ApiError("CANNOT_CANCEL", "This booking cannot be cancelled.")));
        }
    }

    private Guid? ParseGuestId()
    {
        var str = User.FindFirst("sub")?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return str is not null && Guid.TryParse(str, out var id) ? id : null;
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
