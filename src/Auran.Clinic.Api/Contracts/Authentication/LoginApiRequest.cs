using System.ComponentModel.DataAnnotations;

namespace Auran.Clinic.Api.Contracts.Authentication;

public sealed class LoginApiRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; init; }

    [Required]
    [StringLength(256)]
    public string? Password { get; init; }
}
