using System.ComponentModel.DataAnnotations;

namespace Auran.Clinic.Api.Contracts.Clinics;

public sealed class SetClinicStatusApiRequest
{
    [Required]
    public bool? IsActive { get; set; }
}
