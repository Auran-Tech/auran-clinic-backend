using System.ComponentModel.DataAnnotations;
using Auran.Clinic.Api.Validation;

namespace Auran.Clinic.Api.Contracts.Clinics;

public sealed class InitialAdminApiRequest
{
    [Required]
    [StringLength(200)]
    public string? FullName { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [Required]
    [StrongPassword]
    public string? Password { get; set; }
}
