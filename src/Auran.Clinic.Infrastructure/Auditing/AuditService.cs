using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Auditing;

public sealed class AuditService(
    AuranClinicDbContext dbContext,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var clinicId = auditEvent.ClinicId ?? currentUser.ClinicId;
        if (!clinicId.HasValue || clinicId == Guid.Empty)
            return;

        var actorUserId = auditEvent.ActorUserId ?? currentUser.UserId;
        var httpContext = httpContextAccessor.HttpContext;
        var now = DateTime.UtcNow;

        dbContext.AuditLogs.Add(new AuditLog
        {
            ClinicId = clinicId.Value,
            ActorUserId = actorUserId,
            Action = auditEvent.Action,
            Category = auditEvent.Category,
            EntityType = auditEvent.EntityType,
            EntityId = auditEvent.EntityId,
            Description = auditEvent.Description,
            OccurredAtUtc = now,
            MetadataJson = AuditRedactor.Serialize(auditEvent.Metadata),
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
            CorrelationId = httpContext?.TraceIdentifier,
            CreatedDate = now,
            CreateByUserId = actorUserId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<AuditLogResponse>> SearchAsync(
        AuditLogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AuditLog> query = dbContext.AuditLogs.AsNoTracking();

        if (currentUser.IsSuperUser)
        {
            if (request.ClinicId.HasValue)
                query = query.Where(x => x.ClinicId == request.ClinicId.Value);
        }
        else
        {
            if (!currentUser.ClinicId.HasValue)
                return Empty(request);

            query = query.Where(x => x.ClinicId == currentUser.ClinicId.Value);
        }

        if (request.UserId.HasValue)
            query = query.Where(x => x.ActorUserId == request.UserId.Value);
        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(x => x.Action == request.Action);
        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(x => x.Category == request.Category);
        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(x => x.EntityType == request.EntityType);
        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(x => x.EntityId == request.EntityId);
        if (request.FromUtc.HasValue)
            query = query.Where(x => x.OccurredAtUtc >= request.FromUtc.Value);
        if (request.ToUtc.HasValue)
            query = query.Where(x => x.OccurredAtUtc <= request.ToUtc.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var logs = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var actorIds = logs.Where(x => x.ActorUserId.HasValue)
            .Select(x => x.ActorUserId!.Value)
            .Distinct()
            .ToList();
        var actorNames = await dbContext.Users.AsNoTracking()
            .Where(x => actorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);

        return new PaginatedResponse<AuditLogResponse>
        {
            Data = logs.Select(x => Map(x, actorNames)).ToList(),
            Setting = new PaginationInfo
            {
                TotalCount = totalCount,
                RowCount = request.PageSize,
                CurrentPage = request.Page
            }
        };
    }

    public async Task<AuditLogResponse?> GetByIdAsync(Guid auditLogId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AuditLogs.AsNoTracking().Where(x => x.Id == auditLogId);
        if (!currentUser.IsSuperUser)
        {
            if (!currentUser.ClinicId.HasValue)
                return null;

            query = query.Where(x => x.ClinicId == currentUser.ClinicId.Value);
        }

        var log = await query.SingleOrDefaultAsync(cancellationToken);
        if (log is null)
            return null;

        string? actorName = null;
        if (log.ActorUserId.HasValue)
        {
            actorName = await dbContext.Users.AsNoTracking()
                .Where(x => x.Id == log.ActorUserId.Value)
                .Select(x => x.FullName)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return Map(log, actorName is null
            ? new Dictionary<Guid, string>()
            : new Dictionary<Guid, string> { [log.ActorUserId!.Value] = actorName });
    }

    private static AuditLogResponse Map(AuditLog log, IReadOnlyDictionary<Guid, string> actorNames) => new()
    {
        Id = log.Id,
        ClinicId = log.ClinicId,
        ActorUserId = log.ActorUserId,
        ActorName = log.ActorUserId.HasValue && actorNames.TryGetValue(log.ActorUserId.Value, out var name) ? name : null,
        Action = log.Action,
        Category = log.Category,
        EntityType = log.EntityType,
        EntityId = log.EntityId,
        Description = log.Description,
        OccurredAtUtc = log.OccurredAtUtc,
        MetadataJson = log.MetadataJson,
        IpAddress = log.IpAddress,
        UserAgent = log.UserAgent,
        CorrelationId = log.CorrelationId
    };

    private static PaginatedResponse<AuditLogResponse> Empty(AuditLogSearchRequest request) => new()
    {
        Data = new List<AuditLogResponse>(),
        Setting = new PaginationInfo
        {
            TotalCount = 0,
            RowCount = request.PageSize,
            CurrentPage = request.Page
        }
    };
}
