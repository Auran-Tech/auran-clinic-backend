using System.ComponentModel.DataAnnotations;
using Auran.Clinic.Api.Validation;

namespace Auran.Clinic.Api.Contracts.Clinics;

public sealed class CreateClinicApiRequest : ClinicProfileApiRequest
{
    [Required]
    [CodePrefix]
    public string? CodePrefix { get; set; }

    [Required]
    public InitialAdminApiRequest? Admin { get; set; }
}
