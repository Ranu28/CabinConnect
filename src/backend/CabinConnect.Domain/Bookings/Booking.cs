namespace CabinConnect.Domain.Bookings;

public sealed class Booking
{
    public required Guid          Id         { get; init; }
    public required Guid          CabinId    { get; init; }
    public required Guid          GuestId    { get; init; }
    public          Guid?         HoldId     { get; init; }
    public required DateOnly      CheckIn    { get; init; }
    public required DateOnly      CheckOut   { get; init; }
    public required BookingStatus Status     { get; init; }
    public required decimal       TotalPrice { get; init; }
    public required string        Currency   { get; init; }
}
