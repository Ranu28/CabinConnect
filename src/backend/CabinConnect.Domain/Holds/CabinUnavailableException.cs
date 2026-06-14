namespace CabinConnect.Domain.Holds;

public sealed class CabinUnavailableException : Exception
{
    public CabinUnavailableException() : base("The cabin is not available for the requested dates.") { }
}
