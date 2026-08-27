using Auran.Clinic.Application.Models;

namespace Auran.Clinic.Application.Auditing;

public interface IAuditService
{
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<AuditLogResponse>> SearchAsync(AuditLogSearchRequest request, CancellationToken cancellationToken = default);
    Task<AuditLogResponse?> GetByIdAsync(Guid auditLogId, CancellationToken cancellationToken = default);
}
