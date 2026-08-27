using System.Text.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Domain.Common;
using Auran.Clinic.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Auran.Clinic.Infrastructure.Auditing;

public sealed class AuditSaveChangesInterceptor(
    ICurrentUser currentUser,
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
        var entries = dbContext.ChangeTracker.Entries<BaseEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(entry => entry.Entity is not AuditLog and not Permission and not RefreshToken)
            .ToList();

        var auditLogs = new List<AuditLog>();
        foreach (var entry in entries)
        {
            ApplyBaseEntityMetadata(entry, now);

            var clinicId = ResolveClinicId(entry.Entity);
            if (!clinicId.HasValue || clinicId == Guid.Empty)
                continue;

            var action = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => "Unknown"
            };

            var metadata = BuildMetadata(entry);
            var httpContext = httpContextAccessor.HttpContext;

            auditLogs.Add(new AuditLog
            {
                ClinicId = clinicId.Value,
                ActorUserId = currentUser.UserId,
                Action = action,
                Category = entry.Entity.GetType().Name,
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id == Guid.Empty ? null : entry.Entity.Id.ToString(),
                Description = $"{entry.Entity.GetType().Name} {action.ToLowerInvariant()} operation.",
                OccurredAtUtc = now,
                MetadataJson = JsonSerializer.Serialize(metadata),
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                CorrelationId = httpContext?.TraceIdentifier,
                CreatedDate = now,
                CreateByUserId = currentUser.UserId
            });
        }

        if (auditLogs.Count > 0)
            dbContext.Set<AuditLog>().AddRange(auditLogs);
    }

    private void ApplyBaseEntityMetadata(EntityEntry<BaseEntity> entry, DateTime now)
    {
        if (entry.State == EntityState.Added)
        {
            if (entry.Entity.Id == Guid.Empty)
                entry.Entity.Id = Guid.NewGuid();

            if (entry.Entity.CreatedDate == default)
                entry.Entity.CreatedDate = now;

            entry.Entity.CreateByUserId ??= currentUser.UserId;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Entity.UpdatedDate = now;
            entry.Entity.UpdatedByUserId = currentUser.UserId;
        }
    }

    private Guid? ResolveClinicId(BaseEntity entity)
    {
        if (entity is Clinic clinic)
            return clinic.Id;

        if (entity is ClinicEntity clinicEntity)
            return clinicEntity.ClinicId;

        return currentUser.ClinicId;
    }

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

        return new
        {
            ChangedFields = changedFields,
            OldValues = oldValues,
            NewValues = newValues
        };
    }
}
