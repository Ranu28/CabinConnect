using CabinConnect.Domain.Bookings;
using CabinConnect.Domain.Cabins;

namespace CabinConnect.Domain.Search;

public sealed class CabinWithAvailabilityData
{
    public required Cabin Cabin { get; init; }
    // Bookings that overlap the requested date range (Confirmed or Pending status only)
    public required IReadOnlyList<Booking> OverlappingBookings { get; init; }
    // Blackout dates that overlap the requested date range
    public required IReadOnlyList<BlackoutDate> OverlappingBlackouts { get; init; }
}
