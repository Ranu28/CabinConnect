using System.Security.Claims;
using CabinConnect.Api.Models;
using CabinConnect.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CabinConnect.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private static readonly HashSet<string> _allowedSelfRoles =
        new(StringComparer.OrdinalIgnoreCase) { "guest", "host" };

    private readonly IUserProfileRepository _profileRepository;

    public MeController(IUserProfileRepository profileRepository)
        => _profileRepository = profileRepository;

    // GET /api/me/profile — returns the current user's role and display name.
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = ParseUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Failure(
                new ApiError("UNAUTHORIZED", "Valid authentication is required.")));

        var profile = await _profileRepository.GetByIdAsync(userId.Value, ct);
        if (profile is null)
            return NotFound(ApiResponse<object>.Failure(
                new ApiError("PROFILE_NOT_FOUND", "No profile found. Complete registration to continue.")));

        return Ok(ApiResponse<object>.Success(new
        {
            id          = profile.Id,
            role        = profile.Role,
            displayName = profile.DisplayName
        }));
    }

    // POST /api/me/profile — creates the profile after Supabase Auth signup.
    // Only "guest" and "host" are self-assignable; "admin" must be granted via the DB.
    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile(
        [FromBody] CreateProfileRequest request,
        CancellationToken ct)
    {
        var userId = ParseUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Failure(
                new ApiError("UNAUTHORIZED", "Valid authentication is required.")));

        if (!_allowedSelfRoles.Contains(request.Role))
            return BadRequest(ApiResponse<object>.Failure(
                new ApiError("INVALID_ROLE",
                    $"Role must be 'guest' or 'host'. Got '{request.Role}'.")));

        try
        {
            await _profileRepository.CreateAsync(
                userId.Value,
                request.Role.ToLowerInvariant(),
                request.DisplayName.Trim(),
                ct);
        }
        catch (InvalidOperationException)
        {
            return Conflict(ApiResponse<object>.Failure(
                new ApiError("PROFILE_EXISTS", "A profile already exists for this user.")));
        }

        return CreatedAtAction(nameof(GetProfile), new
        {
            id          = userId.Value,
            role        = request.Role.ToLowerInvariant(),
            displayName = request.DisplayName.Trim()
        });
    }

    private Guid? ParseUserId()
    {
        var str = User.FindFirst("sub")?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return str is not null && Guid.TryParse(str, out var id) ? id : null;
    }
}
