using System.ComponentModel.DataAnnotations;

namespace CabinConnect.Api.Models;

public sealed record CreateProfileRequest(
    [Required, StringLength(100, MinimumLength = 1)] string DisplayName,
    [Required] string Role);
