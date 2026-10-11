using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Auditing;

public sealed class AuditReadService(AuranClinicDbContext dbContext) : IAuditReadService
{
    public async Task<IReadOnlyList<AuditLogListItemResponse>> SearchAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var logs = dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            logs = logs.Where(log => log.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var entityType = query.EntityType.Trim();
            logs = logs.Where(log => log.EntityType.Contains(entityType));
        }

        if (query.ActorUserId.HasValue)
            logs = logs.Where(log => log.ActorUserId == query.ActorUserId.Value);

        if (query.FromUtc.HasValue)
            logs = logs.Where(log => log.OccurredAtUtc >= query.FromUtc.Value);

        if (query.ToUtc.HasValue)
            logs = logs.Where(log => log.OccurredAtUtc <= query.ToUtc.Value);

        var boundedTake = Math.Clamp(query.Take, 1, 200);

        return await (
            from log in logs
            join user in dbContext.Users.AsNoTracking() on log.ActorUserId equals user.Id
            orderby log.OccurredAtUtc descending
            select new AuditLogListItemResponse(
                log.Id,
                log.ActorUserId,
                user.FullName,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.OccurredAtUtc,
                log.MetadataJson,
                log.IpAddress))
            .Take(boundedTake)
            .ToListAsync(cancellationToken);
    }
}
