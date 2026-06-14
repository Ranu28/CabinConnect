namespace CabinConnect.Domain.Holds;

public sealed class HoldOwnershipException : Exception
{
    public HoldOwnershipException() : base("The hold does not belong to the authenticated guest.") { }
}
