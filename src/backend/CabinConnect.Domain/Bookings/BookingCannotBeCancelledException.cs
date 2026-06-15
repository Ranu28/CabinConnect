namespace CabinConnect.Domain.Bookings;

public sealed class BookingCannotBeCancelledException : Exception
{
    public BookingCannotBeCancelledException(BookingStatus status)
        : base($"A booking in status {status} cannot be cancelled.") { }
}
