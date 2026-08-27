using Auran.Clinic.Domain.Common;
using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class AuditLog : BaseEntity
{
    public AuditScope Scope { get; set; }
    public Guid? ClinicId { get; set; }
    public ActorType ActorType { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorIdentityUserId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? ActorEmail { get; set; }
    public required string Action { get; set; }
    public required string Category { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Description { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? MetadataJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
}
