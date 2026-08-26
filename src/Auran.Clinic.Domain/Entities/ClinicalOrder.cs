namespace Auran.Clinic.Domain.Entities;

public class ClinicalOrder : ClinicEntity
{
    public Guid VisitId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid CreatedByUserId { get; set; }
}
