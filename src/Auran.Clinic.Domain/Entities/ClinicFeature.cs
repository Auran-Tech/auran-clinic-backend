namespace Auran.Clinic.Domain.Entities;

public class ClinicFeature : ClinicEntity
{
    public Guid FeatureDefinitionId { get; set; }
    public bool IsEnabled { get; set; }
    public string? ConfigurationJson { get; set; }
}
