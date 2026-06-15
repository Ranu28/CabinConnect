namespace CabinConnect.Domain.Users;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a profile. Role must be "guest" or "host" — "admin" cannot be self-assigned.
    /// Throws <see cref="InvalidOperationException"/> if a profile already exists for the user.
    /// </summary>
    Task CreateAsync(Guid userId, string role, string displayName, CancellationToken ct = default);
}
