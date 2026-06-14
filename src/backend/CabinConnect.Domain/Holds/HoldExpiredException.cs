namespace CabinConnect.Domain.Holds;

public sealed class HoldExpiredException : Exception
{
    public HoldExpiredException() : base("The hold has expired.") { }
}
