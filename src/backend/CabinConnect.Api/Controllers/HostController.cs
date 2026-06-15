using System.Security.Claims;
using CabinConnect.Api.Models;
using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CabinConnect.Api.Controllers;

[ApiController]
[Route("api/host")]
[Authorize]
public sealed class HostController : ControllerBase
{
    private readonly BookingService _bookingService;
    private readonly IUserProfileRepository _profileRepository;

    public HostController(BookingService bookingService, IUserProfileRepository profileRepository)
    {
        _bookingService = bookingService;
        _profileRepository = profileRepository;
    }

    // AC-1/AC-2/AC-3/AC-4/AC-5: paginated booking list for all Cabins owned by this Host
    [HttpGet("bookings")]
    public async Task<IActionResult> GetHostBookings(
        [FromQuery] string?  status   = null,
        [FromQuery] int      page     = 1,
        [FromQuery] int      pageSize = 10,
        CancellationToken    ct       = default)
    {
        var hostId = ParseHostId();
        if (hostId is null)
            return Unauthorized(ApiResponse<object>.Failure(
                new ApiError("UNAUTHORIZED", "Valid authentication is required.")));

        // Role check: only hosts and admins may access this endpoint.
        var profile = await _profileRepository.GetByIdAsync(hostId.Value, ct);
        if (profile is null || profile.Role is not ("host" or "admin"))
            return StatusCode(403, ApiResponse<object>.Failure(
                new ApiError("FORBIDDEN", "Host or Admin role is required.")));

        if (page < 1 || pageSize < 1 || pageSize > 50)
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_PARAMETERS", "page ≥ 1; pageSize between 1 and 50.")));

        // AC-2: parse optional status filter; unknown status value → 400
        BookingStatus? statusFilter = null;
        if (status is not null)
        {
            if (!Enum.TryParse<BookingStatus>(status, ignoreCase: true, out var parsed))
                return BadRequest(ApiResponse<object>.Failure(
                    new ApiError("INVALID_PARAMETERS",
                        $"Unknown status '{status}'. Valid values: Pending, Confirmed, Cancelled, Completed, NoShow.")));
            statusFilter = parsed;
        }

        var result = await _bookingService.GetHostBookingsAsync(
            hostId.Value, statusFilter, page, pageSize, ct);

        return Ok(ApiResponse<object>.Success(new
        {
            items = result.Items.Select(b => new
            {
                bookingId    = b.BookingId,
                cabinId      = b.CabinId,
                cabinName    = b.CabinName,
                guestRef     = b.GuestId.ToString()[..8] + "…", // truncated guest ID (not PII)
                checkIn      = b.CheckIn.ToString("yyyy-MM-dd"),
                checkOut     = b.CheckOut.ToString("yyyy-MM-dd"),
                totalPrice   = b.TotalPrice,
                currency     = b.Currency,
                status       = b.Status.ToString()
            }),
            page       = result.Page,
            pageSize   = result.PageSize,
            totalCount = result.TotalCount
        }));
    }

    private Guid? ParseHostId()
    {
        var str = User.FindFirst("sub")?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return str is not null && Guid.TryParse(str, out var id) ? id : null;
    }
}
