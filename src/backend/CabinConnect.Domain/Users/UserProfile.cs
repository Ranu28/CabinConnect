namespace CabinConnect.Domain.Users;

public sealed record UserProfile(Guid Id, string Role, string DisplayName);
