namespace CabinConnect.Domain.Holds;

public sealed class HoldNotActiveException : Exception
{
    public HoldNotActiveException() : base("The hold is not active.") { }
}
