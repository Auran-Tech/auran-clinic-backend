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

public class ClinicalFieldOption : ClinicEntity
{
    public Guid ClinicalFieldId { get; set; }
    public required string Label { get; set; }
    public required string Value { get; set; }
    public int SortOrder { get; set; }
}

public class ClinicalMeasurement : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid? VisitId { get; set; }
    public Guid ClinicalFieldId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateOnly? DateValue { get; set; }
    public string? JsonValue { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid RecordedByUserId { get; set; }
}
