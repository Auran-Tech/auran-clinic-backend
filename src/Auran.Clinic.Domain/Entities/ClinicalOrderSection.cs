namespace Auran.Clinic.Domain.Entities;

public class ClinicalOrderSection : ClinicEntity
{
    public Guid ClinicalOrderId { get; set; }
    public Guid SectionDefinitionId { get; set; }
    public int SortOrder { get; set; }
    public string? TextValue { get; set; }
}
