namespace Auran.Clinic.Domain.Entities;

public class ClinicalOrderAttachment : ClinicEntity
{
    public Guid ClinicalOrderId { get; set; }
    public Guid? ClinicalOrderSectionId { get; set; }
    public Guid FileId { get; set; }
}
