namespace Auran.Clinic.Domain.Entities;

public class VisitSession : ClinicEntity
{
    public Guid VisitId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
}
