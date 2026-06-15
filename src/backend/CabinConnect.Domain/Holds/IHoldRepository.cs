namespace CabinConnect.Domain.Holds;

public interface IHoldRepository
{
    /// <summary>
    /// Creates a Hold atomically: cancels the guest's previous Active Hold (EC-011),
    /// validates cabin availability within a transaction, and inserts the new Hold.
    /// </summary>
    Task<Hold> PlaceHoldAsync(
        Guid cabinId, Guid guestId, DateOnly checkIn, DateOnly checkOut,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels all Active holds whose expiresAt has passed.
    /// Returns the number of holds cancelled.
    /// </summary>
    Task<int> CancelExpiredHoldsAsync(CancellationToken ct = default);
}
