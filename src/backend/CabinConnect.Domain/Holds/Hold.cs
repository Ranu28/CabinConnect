namespace CabinConnect.Domain.Holds;

public sealed class Hold
{
    public required Guid            Id        { get; init; }
    public required Guid            CabinId   { get; init; }
    public required Guid            GuestId   { get; init; }
    public required DateOnly        CheckIn   { get; init; }
    public required DateOnly        CheckOut  { get; init; }
    public required DateTimeOffset  ExpiresAt { get; init; }
    public required HoldStatus      Status    { get; init; }
}
