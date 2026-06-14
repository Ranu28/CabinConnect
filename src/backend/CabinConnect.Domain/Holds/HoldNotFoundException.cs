namespace CabinConnect.Domain.Holds;

public sealed class HoldNotFoundException : Exception
{
    public HoldNotFoundException(Guid holdId)
        : base($"Hold {holdId} was not found.") { }
}
