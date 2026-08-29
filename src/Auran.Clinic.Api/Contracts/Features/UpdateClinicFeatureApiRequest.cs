using System.ComponentModel.DataAnnotations;
using Auran.Clinic.Api.Validation;

namespace Auran.Clinic.Api.Contracts.Features;

public sealed class UpdateClinicFeatureApiRequest
{
    [Required]
    [StringLength(100)]
    [KnownFeatureCode]
    public string? Code { get; init; }

    public bool IsEnabled { get; init; }

    [StringLength(8000)]
    public string? ConfigurationJson { get; init; }
}
