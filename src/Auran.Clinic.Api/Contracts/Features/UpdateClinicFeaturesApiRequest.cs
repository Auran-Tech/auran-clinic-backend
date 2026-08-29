using System.ComponentModel.DataAnnotations;
using Auran.Clinic.Api.Validation;

namespace Auran.Clinic.Api.Contracts.Features;

public sealed class UpdateClinicFeaturesApiRequest
{
    [Required]
    [MinLength(1)]
    [UniqueStringPropertyValues(nameof(UpdateClinicFeatureApiRequest.Code), ErrorMessage = "Feature codes must be unique.")]
    public List<UpdateClinicFeatureApiRequest>? Features { get; init; }
}
