namespace CabinConnect.Domain.Bookings;

public sealed class BookingNotFoundException : Exception
{
    public BookingNotFoundException(Guid bookingId)
        : base($"Booking {bookingId} not found.") { }
}
