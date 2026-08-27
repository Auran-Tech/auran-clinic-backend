namespace Auran.Clinic.Application.Features;

public sealed class UpdateClinicFeaturesRequest
{
    public List<UpdateClinicFeatureItem> Features { get; init; } = new();
}
