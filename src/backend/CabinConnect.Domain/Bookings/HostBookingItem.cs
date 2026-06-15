namespace CabinConnect.Domain.Bookings;

public sealed record HostBookingItem(
    Guid          BookingId,
    Guid          CabinId,
    string        CabinName,
    Guid          GuestId,
    DateOnly      CheckIn,
    DateOnly      CheckOut,
    decimal       TotalPrice,
    string        Currency,
    BookingStatus Status);
