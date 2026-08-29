using System.ComponentModel.DataAnnotations;

namespace Auran.Clinic.Api.Contracts.Authentication;

public sealed class RefreshTokenApiRequest
{
    [Required]
    [StringLength(2048)]
    public string? RefreshToken { get; init; }
}
