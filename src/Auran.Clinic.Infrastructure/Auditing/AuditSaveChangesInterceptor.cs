using System.Text.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Domain.Common;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ClinicEntityType = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Auditing;

public sealed class AuditSaveChangesInterceptor(
    ICurrentActor currentActor,
    IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? dbContext)
    {
        if (dbContext is null)
            return;

        var now = DateTime.UtcNow;
        var actorId = ResolveActorId();
        var entries = dbContext.ChangeTracker.Entries<BaseEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(entry => entry.Entity is not AuditLog and not RefreshToken and not PlatformRefreshToken)
            .ToList();

        var auditLogs = new List<AuditLog>();
        foreach (var entry in entries)
        {
            ApplyBaseEntityMetadata(entry, now, actorId);
            var (scope, clinicId) = ResolveScope(entry.Entity);
            var action = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => "Unknown"
            };

            var httpContext = httpContextAccessor.HttpContext;
            var actorType = currentActor.IsAuthenticated ? currentActor.ActorType : ActorType.System;
            auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                Scope = scope,
                ClinicId = clinicId,
                ActorType = actorType,
                ActorId = actorId,
                ActorIdentityUserId = currentActor.IdentityUserId,
                ActorDisplayName = currentActor.DisplayName,
                ActorEmail = currentActor.Email,
                Action = action,
                Category = entry.Entity.GetType().Name,
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id == Guid.Empty ? null : entry.Entity.Id.ToString(),
                Description = $"{entry.Entity.GetType().Name} {action.ToLowerInvariant()} operation.",
                OccurredAtUtc = now,
                MetadataJson = JsonSerializer.Serialize(BuildMetadata(entry)),
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                CorrelationId = httpContext?.TraceIdentifier,
                CreatedDate = now,
                CreateByUserId = actorId
            });
        }

        if (auditLogs.Count > 0)
            dbContext.Set<AuditLog>().AddRange(auditLogs);
    }

    private static void ApplyBaseEntityMetadata(EntityEntry<BaseEntity> entry, DateTime now, Guid? actorId)
    {
        if (entry.State == EntityState.Added)
        {
            if (entry.Entity.Id == Guid.Empty)
                entry.Entity.Id = Guid.NewGuid();
            if (entry.Entity.CreatedDate == default)
                entry.Entity.CreatedDate = now;
            entry.Entity.CreateByUserId ??= actorId;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Entity.UpdatedDate = now;
            entry.Entity.UpdatedByUserId = actorId;
        }
    }

    private (AuditScope Scope, Guid? ClinicId) ResolveScope(BaseEntity entity)
    {
        if (entity is ClinicEntityType clinic)
            return (AuditScope.Clinic, clinic.Id);
        if (entity is ClinicEntity clinicEntity)
            return (AuditScope.Clinic, clinicEntity.ClinicId);

        return (AuditScope.Platform, null);
    }

    private Guid? ResolveActorId() => currentActor.ActorType switch
    {
        ActorType.Platform => currentActor.PlatformUserId,
        ActorType.Clinic => currentActor.ClinicUserId,
        _ => null
    };

    private static object BuildMetadata(EntityEntry<BaseEntity> entry)
    {
        var changedFields = new List<string>();
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (entry.State == EntityState.Added)
            {
                newValues[name] = AuditRedactor.Sanitize(name, property.CurrentValue);
                continue;
            }
            if (entry.State == EntityState.Deleted)
            {
                oldValues[name] = AuditRedactor.Sanitize(name, property.OriginalValue);
                continue;
            }
            if (!property.IsModified)
                continue;

            changedFields.Add(name);
            oldValues[name] = AuditRedactor.Sanitize(name, property.OriginalValue);
            newValues[name] = AuditRedactor.Sanitize(name, property.CurrentValue);
        }

        return new { ChangedFields = changedFields, OldValues = oldValues, NewValues = newValues };
    }
}
