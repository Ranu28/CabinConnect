namespace CabinConnect.Domain.Holds;

public sealed class HoldService
{
    private readonly IHoldRepository _repository;

    public HoldService(IHoldRepository repository)
    {
        _repository = repository;
    }

    public Task<Hold> PlaceHoldAsync(
        Guid cabinId, Guid guestId, DateOnly checkIn, DateOnly checkOut,
        CancellationToken ct = default)
        => _repository.PlaceHoldAsync(cabinId, guestId, checkIn, checkOut, ct);
}
