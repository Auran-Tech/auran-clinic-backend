using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Auditing;

public sealed class AuditLogResponse
{
    public Guid Id { get; init; }
    public AuditScope Scope { get; init; }
    public Guid? ClinicId { get; init; }
    public ActorType ActorType { get; init; }
    public Guid? ActorId { get; init; }
    public string? ActorDisplayName { get; init; }
    public string? ActorEmail { get; init; }
    public required string Action { get; init; }
    public required string Category { get; init; }
    public required string EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? Description { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string? MetadataJson { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? CorrelationId { get; init; }
}
