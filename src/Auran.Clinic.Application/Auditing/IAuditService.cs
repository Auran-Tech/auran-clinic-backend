namespace Auran.Clinic.Application.Auditing;

public interface IAuditService
{
    Task WriteAsync(
        string action,
        string entityType,
        string? entityId = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogResponse>> GetRecentAsync(
        int take = 100,
        CancellationToken cancellationToken = default);
}

public sealed record AuditLogResponse(
    Guid Id,
    Guid ActorUserId,
    string Action,
    string EntityType,
    string? EntityId,
    DateTime OccurredAtUtc,
    string? MetadataJson,
    string? IpAddress);
