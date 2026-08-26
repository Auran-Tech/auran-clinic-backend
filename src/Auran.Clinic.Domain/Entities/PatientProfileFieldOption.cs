namespace Auran.Clinic.Domain.Entities;

public class PatientProfileFieldOption : ClinicEntity
{
    public Guid FieldId { get; set; }
    public required string Label { get; set; }
    public required string Value { get; set; }
    public int SortOrder { get; set; }
}
