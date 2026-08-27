namespace Auran.Clinic.Application.Features;

public sealed class UpdateClinicFeatureItem
{
    public required string Code { get; init; }
    public bool IsEnabled { get; init; }
    public string? ConfigurationJson { get; init; }
}
