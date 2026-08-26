using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class PatientProfileField : ClinicEntity
{
    public Guid SectionId { get; set; }
    public required string Label { get; set; }
    public DynamicFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}
