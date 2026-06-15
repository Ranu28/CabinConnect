using CabinConnect.Domain.Common;
using CabinConnect.Domain.Rates;

namespace CabinConnect.Domain.Bookings;

public interface IBookingRepository
{
    /// <summary>
    /// Confirms a Booking from an Active Hold atomically (EC-012):
    /// locks the Hold, validates ownership and state, computes the frozen price,
    /// inserts the Booking, and marks the Hold as Consumed — all in one transaction.
    /// </summary>
    Task<(Booking Booking, RateBreakdown PriceBreakdown)> ConfirmHoldAsync(
        Guid holdId, Guid guestId, CancellationToken ct = default);

    /// <summary>
    /// Cancels a booking owned by the given Guest.
    /// Throws <see cref="BookingNotFoundException"/> when the booking does not exist or is owned by
    /// a different Guest — intentionally indistinguishable to prevent information leaks (EC-007).
    /// Throws <see cref="BookingCannotBeCancelledException"/> for Completed or NoShow bookings.
    /// Idempotent: a booking that is already Cancelled returns without error.
    /// </summary>
    Task CancelBookingAsync(Guid bookingId, Guid guestId, CancellationToken ct = default);

    /// <summary>Returns a page of the Guest's Bookings across all statuses, sorted by check-in descending.</summary>
    Task<PagedResult<GuestBookingItem>> GetGuestBookingsAsync(
        Guid guestId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Returns a single Booking for the given Guest, or null when not found or not owned (EC-007).
    /// </summary>
    Task<GuestBookingItem?> GetGuestBookingByIdAsync(
        Guid bookingId, Guid guestId, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated list of Bookings across all Cabins owned by the given Host.
    /// Optionally filtered by status. Only returns bookings for cabins where host_id = hostId (AC-3).
    /// </summary>
    Task<PagedResult<HostBookingItem>> GetHostBookingsAsync(
        Guid hostId, BookingStatus? status, int page, int pageSize, CancellationToken ct = default);
}
