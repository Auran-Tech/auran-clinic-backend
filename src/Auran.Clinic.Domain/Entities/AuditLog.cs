namespace Auran.Clinic.Domain.Entities;

public class AuditLog : ClinicEntity
{
    public Guid? ActorUserId { get; set; }
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
