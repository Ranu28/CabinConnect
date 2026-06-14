namespace CabinConnect.Domain.Cabins;

public sealed class BlackoutDate
{
    public required Guid Id { get; init; }
    public required Guid CabinId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}
