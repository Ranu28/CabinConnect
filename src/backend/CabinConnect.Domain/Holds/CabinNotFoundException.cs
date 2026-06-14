namespace CabinConnect.Domain.Holds;

public sealed class CabinNotFoundException : Exception
{
    public CabinNotFoundException(Guid cabinId)
        : base($"Cabin {cabinId} was not found or is not published.") { }
}
