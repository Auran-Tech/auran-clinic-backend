using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Auditing;

public sealed class AuditService(
    AuranClinicDbContext dbContext,
    ICurrentActor currentActor,
    IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var scope = auditEvent.Scope ?? ResolveScope(auditEvent.ClinicId);
        var clinicId = auditEvent.ClinicId ?? (scope == AuditScope.Clinic ? currentActor.ClinicId : null);
        if (scope == AuditScope.Clinic && (!clinicId.HasValue || clinicId == Guid.Empty))
            return;

        var actorType = auditEvent.ActorType ?? ResolveActorType();
        var actorId = auditEvent.ActorId ?? ResolveActorId(actorType);
        var httpContext = httpContextAccessor.HttpContext;
        var now = DateTime.UtcNow;

        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Scope = scope,
            ClinicId = clinicId,
            ActorType = actorType,
            ActorId = actorId,
            ActorIdentityUserId = auditEvent.ActorIdentityUserId ?? currentActor.IdentityUserId,
            ActorDisplayName = auditEvent.ActorDisplayName ?? currentActor.DisplayName,
            ActorEmail = auditEvent.ActorEmail ?? currentActor.Email,
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
            CreateByUserId = actorId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<AuditLogResponse>> SearchAsync(
        AuditLogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyVisibility(dbContext.AuditLogs.AsNoTracking());

        if (request.Scope.HasValue)
            query = query.Where(x => x.Scope == request.Scope.Value);
        if (request.ClinicId.HasValue && currentActor.ActorType == ActorType.Platform)
            query = query.Where(x => x.ClinicId == request.ClinicId.Value);
        if (request.ActorType.HasValue)
            query = query.Where(x => x.ActorType == request.ActorType.Value);
        if (request.ActorId.HasValue)
            query = query.Where(x => x.ActorId == request.ActorId.Value);
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
        var entities = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<AuditLogResponse>
        {
            Data = entities.Select(Map).ToList(),
            Setting = new PaginationInfo
            {
                TotalCount = totalCount,
                RowCount = request.PageSize,
                CurrentPage = request.Page
            }
        };
    }

    public async Task<AuditLogResponse?> GetByIdAsync(
        Guid auditLogId,
        CancellationToken cancellationToken = default)
    {
        var entity = await ApplyVisibility(dbContext.AuditLogs.AsNoTracking())
            .SingleOrDefaultAsync(x => x.Id == auditLogId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    private IQueryable<AuditLog> ApplyVisibility(IQueryable<AuditLog> query)
    {
        if (!currentActor.IsAuthenticated)
            return query.Where(_ => false);

        if (currentActor.ActorType == ActorType.Platform)
        {
            return query.Where(x =>
                x.Scope == AuditScope.Platform ||
                (x.Scope == AuditScope.Clinic && x.ActorType == ActorType.Platform));
        }

        if (currentActor.ActorType == ActorType.Clinic && currentActor.ClinicId.HasValue)
        {
            var clinicId = currentActor.ClinicId.Value;
            return query.Where(x => x.Scope == AuditScope.Clinic && x.ClinicId == clinicId);
        }

        return query.Where(_ => false);
    }

    private AuditScope ResolveScope(Guid? eventClinicId) =>
        eventClinicId.HasValue || currentActor.ActorType == ActorType.Clinic
            ? AuditScope.Clinic
            : AuditScope.Platform;

    private ActorType ResolveActorType() =>
        currentActor.IsAuthenticated ? currentActor.ActorType : ActorType.System;

    private Guid? ResolveActorId(ActorType actorType) => actorType switch
    {
        ActorType.Platform => currentActor.PlatformUserId,
        ActorType.Clinic => currentActor.ClinicUserId,
        _ => null
    };

    private static AuditLogResponse Map(AuditLog log) => new()
    {
        Id = log.Id,
        Scope = log.Scope,
        ClinicId = log.ClinicId,
        ActorType = log.ActorType,
        ActorId = log.ActorId,
        ActorDisplayName = log.ActorDisplayName,
        ActorEmail = log.ActorEmail,
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
}
