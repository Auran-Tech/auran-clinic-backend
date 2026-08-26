namespace Auran.Clinic.Domain.Entities;

public class ClinicalFieldOption : ClinicEntity
{
    public Guid ClinicalFieldId { get; set; }
    public required string Label { get; set; }
    public required string Value { get; set; }
    public int SortOrder { get; set; }
}
