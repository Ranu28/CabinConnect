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
}
