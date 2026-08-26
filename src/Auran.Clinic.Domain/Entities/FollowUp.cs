using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class FollowUp : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid VisitId { get; set; }
    public Guid DoctorId { get; set; }
    public required string Recommendation { get; set; }
    public int? RecommendedAfterDays { get; set; }
    public DateOnly? RecommendedDate { get; set; }
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Open;
}
