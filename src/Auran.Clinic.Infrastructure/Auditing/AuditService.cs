using System.Text.Json;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Auditing;

public sealed class AuditService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task WriteAsync(
        string action,
        string entityType,
        string? entityId = null,
        IReadOnlyDictionary<string, object?>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated ||
            currentUserContext.UserId is not Guid userId ||
            currentUserContext.ClinicId is not Guid clinicId)
        {
            throw new InvalidOperationException("An authenticated clinic user is required to write an audit event.");
        }

        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Audit action and entity type are required.");

        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            ActorUserId = userId,
            Action = action.Trim(),
            EntityType = entityType.Trim(),
            EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim(),
            OccurredAtUtc = DateTime.UtcNow,
            MetadataJson = metadata is null || metadata.Count == 0
                ? null
                : JsonSerializer.Serialize(AuditRedactor.Redact(metadata)),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedDate = DateTime.UtcNow,
            CreateByUserId = userId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogResponse>> GetRecentAsync(
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated || !currentUserContext.ClinicId.HasValue)
            throw new InvalidOperationException("An authenticated clinic user is required to read audit events.");

        var boundedTake = Math.Clamp(take, 1, 200);
        return await dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.OccurredAtUtc)
            .Take(boundedTake)
            .Select(log => new AuditLogResponse(
                log.Id,
                log.ActorUserId,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.OccurredAtUtc,
                log.MetadataJson,
                log.IpAddress))
            .ToListAsync(cancellationToken);
    }
}
