using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class FeatureDefinition : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsDefaultEnabled { get; set; }
}
