namespace Auran.Clinic.Application.Features;

public sealed record FeatureDefinition(
    string Code,
    string Name,
    string Description,
    bool IsDefaultEnabled);
