using Auran.Clinic.Domain.Common;
using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class FileRecord : ClinicEntity
{
    public required string OriginalName { get; set; }
    public required string StoredName { get; set; }
    public required string ContentType { get; set; }
    public long Size { get; set; }
    public required string StorageProvider { get; set; }
    public required string StorageKey { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public Guid UploadedByUserId { get; set; }
}

public class PatientAttachment : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid FileId { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
}

public class ClinicalOrderAttachment : ClinicEntity
{
    public Guid ClinicalOrderId { get; set; }
    public Guid? ClinicalOrderSectionId { get; set; }
    public Guid FileId { get; set; }
}

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

public class ClinicSettings : BaseEntity
{
    public Guid ClinicId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? Locale { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
    public int DocumentationReminderHours { get; set; } = 12;
    public string? PrescriptionHeader { get; set; }
    public string? PrescriptionFooter { get; set; }
    public string? WelcomeButtonText { get; set; }
}

public class AuditLog : ClinicEntity
{
    public Guid ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
}
