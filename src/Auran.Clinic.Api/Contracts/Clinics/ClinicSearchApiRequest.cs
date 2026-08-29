using System.ComponentModel.DataAnnotations;

namespace Auran.Clinic.Api.Contracts.Clinics;

public sealed class ClinicSearchApiRequest
{
    [StringLength(200)]
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
