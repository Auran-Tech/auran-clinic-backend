namespace Auran.Clinic.Application.Features;

public sealed class ClinicFeatureResponse
{
    public Guid FeatureId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
    public string? ConfigurationJson { get; init; }
}
