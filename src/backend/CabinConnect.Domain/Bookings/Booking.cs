namespace CabinConnect.Domain.Bookings;

public sealed class Booking
{
    public required Guid Id { get; init; }
    public required Guid CabinId { get; init; }
    public required DateOnly CheckIn { get; init; }
    public required DateOnly CheckOut { get; init; }
    public required BookingStatus Status { get; init; }
}
