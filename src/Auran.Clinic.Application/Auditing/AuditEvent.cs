namespace Auran.Clinic.Application.Auditing;

public sealed class AuditEvent
{
    public Guid? ClinicId { get; init; }
    public Guid? ActorUserId { get; init; }
    public required string Action { get; init; }
    public required string Category { get; init; }
    public required string EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? Description { get; init; }
    public object? Metadata { get; init; }
}
