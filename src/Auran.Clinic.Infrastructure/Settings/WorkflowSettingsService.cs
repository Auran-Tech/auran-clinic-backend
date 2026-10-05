using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Settings;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Settings;

public sealed class WorkflowSettingsService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IWorkflowSettingsService
{
    public async Task<WorkflowSettingsResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await dbContext.WorkflowStatuses
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var statusMap = statuses.ToDictionary(item => item.Id, item => item.Code);
        var transitions = await dbContext.WorkflowTransitions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new WorkflowSettingsResponse(
            statuses
                .Select(item => new WorkflowStatusSettingsResponse(
                    item.Code,
                    item.Name,
                    item.Color,
                    item.SortOrder,
                    item.IsSystemFinal))
                .ToArray(),
            transitions
                .Where(item => statusMap.ContainsKey(item.FromStatusId) && statusMap.ContainsKey(item.ToStatusId))
                .Select(item => new WorkflowTransitionSettingsResponse(
                    statusMap[item.FromStatusId],
                    statusMap[item.ToStatusId]))
                .ToArray());
    }

    public async Task<(WorkflowSettingsResponse? Settings, string? Error)> SaveAsync(
        SaveWorkflowSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.ClinicId.HasValue || !currentUserContext.UserId.HasValue)
            return (null, "Authentication with clinic scope is required.");

        var clinicId = currentUserContext.ClinicId.Value;
        var userId = currentUserContext.UserId.Value;
        var requestedStatuses = request.Statuses
            .Select(item => new WorkflowStatusSettingsRequest
            {
                Code = item.Code.Trim().ToUpperInvariant(),
                Name = item.Name.Trim(),
                Color = item.Color.Trim(),
                SortOrder = item.SortOrder,
                IsFinal = item.IsFinal
            })
            .ToArray();

        var requestedCodes = requestedStatuses
            .Select(item => item.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var transition in request.Transitions)
        {
            var from = transition.FromCode.Trim().ToUpperInvariant();
            var to = transition.ToCode.Trim().ToUpperInvariant();
            if (!requestedCodes.Contains(from) || !requestedCodes.Contains(to))
                return (null, "Every workflow transition must reference configured status codes.");
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                return (null, "A workflow status cannot transition to itself.");
        }

        var existingStatuses = await dbContext.WorkflowStatuses
            .ToListAsync(cancellationToken);

        var obsoleteStatuses = existingStatuses
            .Where(item => !requestedCodes.Contains(item.Code))
            .ToList();

        if (obsoleteStatuses.Count > 0)
        {
            var obsoleteIds = obsoleteStatuses.Select(item => item.Id).ToArray();
            var referenced = await dbContext.QueueEntries.AsNoTracking()
                .AnyAsync(item => obsoleteIds.Contains(item.WorkflowStatusId), cancellationToken)
                || await dbContext.QueueStatusHistory.AsNoTracking()
                    .AnyAsync(item =>
                        obsoleteIds.Contains(item.ToStatusId) ||
                        (item.FromStatusId.HasValue && obsoleteIds.Contains(item.FromStatusId.Value)),
                        cancellationToken);

            if (referenced)
                return (null, "Workflow statuses already used by queue history cannot be removed.");
        }

        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existingTransitions = await dbContext.WorkflowTransitions.ToListAsync(cancellationToken);
            dbContext.WorkflowTransitions.RemoveRange(existingTransitions);

            foreach (var obsolete in obsoleteStatuses)
                dbContext.WorkflowStatuses.Remove(obsolete);

            foreach (var requested in requestedStatuses)
            {
                var status = existingStatuses.SingleOrDefault(item =>
                    item.Code.Equals(requested.Code, StringComparison.OrdinalIgnoreCase));

                if (status is null)
                {
                    status = new WorkflowStatus
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinicId,
                        Code = requested.Code,
                        Name = requested.Name,
                        Color = requested.Color,
                        SortOrder = requested.SortOrder,
                        IsSystemFinal = requested.IsFinal,
                        CreatedDate = now,
                        CreateByUserId = userId
                    };
                    dbContext.WorkflowStatuses.Add(status);
                    existingStatuses.Add(status);
                }
                else
                {
                    status.Code = requested.Code;
                    status.Name = requested.Name;
                    status.Color = requested.Color;
                    status.SortOrder = requested.SortOrder;
                    status.IsSystemFinal = requested.IsFinal;
                    status.UpdatedDate = now;
                    status.UpdatedByUserId = userId;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var activeByCode = existingStatuses
                .Where(item => requestedCodes.Contains(item.Code))
                .ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var requested in request.Transitions
                         .Select(item => new
                         {
                             From = item.FromCode.Trim().ToUpperInvariant(),
                             To = item.ToCode.Trim().ToUpperInvariant()
                         })
                         .Distinct())
            {
                dbContext.WorkflowTransitions.Add(new WorkflowTransition
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicId,
                    FromStatusId = activeByCode[requested.From].Id,
                    ToStatusId = activeByCode[requested.To].Id,
                    CreatedDate = now,
                    CreateByUserId = userId
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(
                "Settings.WorkflowUpdated",
                nameof(WorkflowStatus),
                null,
                new Dictionary<string, object?>
                {
                    ["statusCount"] = requestedStatuses.Length,
                    ["transitionCount"] = request.Transitions.Count
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return (await GetAsync(cancellationToken), null);
    }
}
