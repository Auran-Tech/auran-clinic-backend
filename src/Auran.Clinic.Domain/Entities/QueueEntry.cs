namespace Auran.Clinic.Domain.Entities;

public class QueueEntry : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid VisitId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid WorkflowStatusId { get; set; }
    public DateTime EntryAtUtc { get; set; }
    public DateTime? ExitAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
