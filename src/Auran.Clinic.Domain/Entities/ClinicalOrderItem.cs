namespace Auran.Clinic.Domain.Entities;

public class ClinicalOrderItem : ClinicEntity
{
    public Guid ClinicalOrderSectionId { get; set; }
    public required string Name { get; set; }
    public string? DetailsJson { get; set; }
}
