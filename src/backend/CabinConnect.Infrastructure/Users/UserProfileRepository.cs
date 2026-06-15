using CabinConnect.Domain.Users;
using Dapper;
using Npgsql;

namespace CabinConnect.Infrastructure.Users;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public UserProfileRepository(NpgsqlDataSource dataSource)
        => _dataSource = dataSource;

    public async Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<UserProfileRow>(
            "SELECT id AS Id, role AS Role, display_name AS DisplayName FROM user_profiles WHERE id = @UserId",
            new { UserId = userId });
        return row is null ? null : new UserProfile(row.Id, row.Role, row.DisplayName);
    }

    public async Task CreateAsync(Guid userId, string role, string displayName, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(
            """
            INSERT INTO user_profiles (id, role, display_name)
            VALUES (@UserId, @Role, @DisplayName)
            ON CONFLICT (id) DO NOTHING
            """,
            new { UserId = userId, Role = role, DisplayName = displayName });

        if (affected == 0)
            throw new InvalidOperationException($"A profile already exists for user {userId}.");
    }

    private sealed record UserProfileRow(Guid Id, string Role, string DisplayName);
}
