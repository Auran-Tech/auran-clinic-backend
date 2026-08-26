using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class PatientProfileSection : ClinicEntity
{
    public required string Name { get; set; }
    public int SortOrder { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class PatientProfileField : ClinicEntity
{
    public Guid SectionId { get; set; }
    public required string Label { get; set; }
    public DynamicFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}

public class PatientProfileFieldOption : ClinicEntity
{
    public Guid FieldId { get; set; }
    public required string Label { get; set; }
    public required string Value { get; set; }
    public int SortOrder { get; set; }
}

public class PatientProfileValue : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid FieldId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateOnly? DateValue { get; set; }
    public Guid? FileId { get; set; }
    public string? JsonValue { get; set; }
}
