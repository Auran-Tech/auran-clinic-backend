namespace Auran.Clinic.Application.Auditing;

public interface IAuditReadService
{
    Task<IReadOnlyList<AuditLogListItemResponse>> SearchAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}
