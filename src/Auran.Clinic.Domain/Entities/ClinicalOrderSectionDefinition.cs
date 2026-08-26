using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class ClinicalOrderSectionDefinition : ClinicEntity
{
    public required string Name { get; set; }
    public ClinicalOrderSectionType SectionType { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
}
