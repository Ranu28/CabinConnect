namespace CabinConnect.Domain.Bookings;

public sealed record GuestBookingItem(
    Guid          BookingId,
    Guid          CabinId,
    string        CabinName,
    DateOnly      CheckIn,
    DateOnly      CheckOut,
    decimal       TotalPrice,
    string        Currency,
    BookingStatus Status);
