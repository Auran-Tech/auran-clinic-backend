using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class ClinicalField : ClinicEntity
{
    public required string Name { get; set; }
    public DynamicFieldType FieldType { get; set; }
    public string? Unit { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}
