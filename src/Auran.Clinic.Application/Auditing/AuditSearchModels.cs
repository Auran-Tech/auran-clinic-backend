namespace Auran.Clinic.Application.Auditing;

public sealed class AuditLogQuery
{
    public int Take { get; init; } = 100;
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public Guid? ActorUserId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public sealed record AuditLogListItemResponse(
    Guid Id,
    Guid ActorUserId,
    string ActorName,
    string Action,
    string EntityType,
    string? EntityId,
    DateTime OccurredAtUtc,
    string? MetadataJson,
    string? IpAddress);
