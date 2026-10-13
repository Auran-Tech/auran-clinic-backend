using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Workflow;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Workflow;

public sealed class WorkflowConfigurationService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IWorkflowConfigurationService
{
    public async Task<WorkflowConfigurationResponse> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await dbContext.WorkflowStatuses
            .AsNoTracking()
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Name)
            .ToListAsync(cancellationToken);

        var statusIds = statuses.Select(status => status.Id).ToArray();

        var usedStatusIds = new HashSet<Guid>();

        if (statusIds.Length > 0)
        {
            var queueStatusIds = await dbContext.QueueEntries.AsNoTracking()
                .Where(entry => statusIds.Contains(entry.WorkflowStatusId))
                .Select(entry => entry.WorkflowStatusId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var historyToStatusIds = await dbContext.QueueStatusHistory.AsNoTracking()
                .Where(history => statusIds.Contains(history.ToStatusId))
                .Select(history => history.ToStatusId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var historyFromStatusIds = await dbContext.QueueStatusHistory.AsNoTracking()
                .Where(history => history.FromStatusId.HasValue && statusIds.Contains(history.FromStatusId.Value))
                .Select(history => history.FromStatusId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var id in queueStatusIds.Concat(historyToStatusIds).Concat(historyFromStatusIds))
                usedStatusIds.Add(id);
        }

        var transitions = await dbContext.WorkflowTransitions
            .AsNoTracking()
            .OrderBy(transition => transition.FromStatusId)
            .ThenBy(transition => transition.ToStatusId)
            .Select(transition => new WorkflowTransitionResponse(
                transition.Id,
                transition.FromStatusId,
                transition.ToStatusId))
            .ToListAsync(cancellationToken);

        return new WorkflowConfigurationResponse(
            statuses
                .Select(status => new WorkflowStatusResponse(
                    status.Id,
                    status.Code,
                    status.Name,
                    status.Color,
                    status.SortOrder,
                    status.IsSystemFinal,
                    usedStatusIds.Contains(status.Id)))
                .ToList(),
            transitions);
    }

    public async Task<WorkflowConfigurationResult> CreateStatusAsync(
        CreateWorkflowStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId, out var clinicId))
            return new WorkflowConfigurationResult(WorkflowConfigurationOutcome.Unauthenticated);

        var normalizedCode = NormalizeCode(request.Code);

        if (await dbContext.WorkflowStatuses.AnyAsync(
                status => status.Code == normalizedCode,
                cancellationToken))
        {
            return new WorkflowConfigurationResult(
                WorkflowConfigurationOutcome.Conflict,
                Error: "Workflow status code already exists.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (request.IsSystemFinal)
        {
            var existingFinals = await dbContext.WorkflowStatuses
                .Where(status => status.IsSystemFinal)
                .ToListAsync(cancellationToken);

            foreach (var existingFinal in existingFinals)
            {
                existingFinal.IsSystemFinal = false;
                existingFinal.UpdatedDate = DateTime.UtcNow;
                existingFinal.UpdatedByUserId = userId;
            }
        }

        var now = DateTime.UtcNow;
        var status = new WorkflowStatus
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Color = request.Color.Trim(),
            SortOrder = request.SortOrder,
            IsSystemFinal = request.IsSystemFinal,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.WorkflowStatuses.Add(status);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "WorkflowStatus.Created",
            nameof(WorkflowStatus),
            status.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["code"] = status.Code,
                ["isSystemFinal"] = status.IsSystemFinal
            },
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new WorkflowConfigurationResult(
            WorkflowConfigurationOutcome.Success,
            await GetAsync(cancellationToken));
    }

    public async Task<WorkflowConfigurationResult> UpdateStatusAsync(
        UpdateWorkflowStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId, out _))
            return new WorkflowConfigurationResult(WorkflowConfigurationOutcome.Unauthenticated);

        var status = await dbContext.WorkflowStatuses
            .SingleOrDefaultAsync(item => item.Id == request.StatusId, cancellationToken);
        if (status is null)
            return new WorkflowConfigurationResult(WorkflowConfigurationOutcome.NotFound);

        var normalizedCode = NormalizeCode(request.Code);
        var duplicateCode = await dbContext.WorkflowStatuses.AsNoTracking()
            .AnyAsync(
                item => item.Id != status.Id && item.Code == normalizedCode,
                cancellationToken);
        if (duplicateCode)
        {
            return new WorkflowConfigurationResult(
                WorkflowConfigurationOutcome.Conflict,
                Error: "Workflow status code already exists.");
        }

        if (status.IsSystemFinal && !request.IsSystemFinal)
        {
            return new WorkflowConfigurationResult(
                WorkflowConfigurationOutcome.ValidationError,
                Error: "The current final status cannot be unset directly. Mark another status as final instead.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (request.IsSystemFinal && !status.IsSystemFinal)
        {
            var existingFinals = await dbContext.WorkflowStatuses
                .Where(item => item.IsSystemFinal && item.Id != status.Id)
                .ToListAsync(cancellationToken);

            foreach (var existingFinal in existingFinals)
            {
                existingFinal.IsSystemFinal = false;
                existingFinal.UpdatedDate = DateTime.UtcNow;
                existingFinal.UpdatedByUserId = userId;
            }
        }

        status.Code = normalizedCode;
        status.Name = request.Name.Trim();
        status.Color = request.Color.Trim();
        status.SortOrder = request.SortOrder;
        status.IsSystemFinal = request.IsSystemFinal;
        status.UpdatedDate = DateTime.UtcNow;
        status.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "WorkflowStatus.Updated",
            nameof(WorkflowStatus),
            status.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["code"] = status.Code,
                ["isSystemFinal"] = status.IsSystemFinal
            },
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new WorkflowConfigurationResult(
            WorkflowConfigurationOutcome.Success,
            await GetAsync(cancellationToken));
    }

    public async Task<WorkflowConfigurationResult> DeleteStatusAsync(
        DeleteWorkflowStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out _, out _))
            return new WorkflowConfigurationResult(WorkflowConfigurationOutcome.Unauthenticated);

        var status = await dbContext.WorkflowStatuses
            .SingleOrDefaultAsync(item => item.Id == request.StatusId, cancellationToken);
        if (status is null)
            return new WorkflowConfigurationResult(WorkflowConfigurationOutcome.NotFound);

        if (status.IsSystemFinal)
        {
            return new WorkflowConfigurationResult(
                WorkflowConfigurationOutcome.ValidationError,
                Error: "The final workflow status cannot be deleted.");
        }

        var isInUse =
            await dbContext.QueueEntries.AsNoTracking()
                .AnyAsync(entry => entry.WorkflowStatusId == status.Id, cancellationToken) ||
            await dbContext.QueueStatusHistory.AsNoTracking()
                .AnyAsync(
                    history =>
                        history.ToStatusId == status.Id ||
                        history.FromStatusId == status.Id,
                    cancellationToken);

        if (isInUse)
        {
            return new WorkflowConfigurationResult(
                WorkflowConfigurationOutcome.Conflict,
                Error: "Workflow status is already referenced by queue history and cannot be deleted.");
        }

        var transitions = await dbContext.WorkflowTransitions
            .Where(transition =>
                transition.FromStatusId == status.Id ||
                transition.ToStatusId == status.Id)
            .ToListAsync(cancellationToken);

        dbContext.WorkflowTransitions.RemoveRange(transitions);
        dbContext.WorkflowStatuses.Remove(status);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "WorkflowStatus.Deleted",
            nameof(WorkflowStatus),
            status.Id.ToString(),
            new Dictionary<string, object?> { ["code"] = status.Code },
            cancellationToken);

        return new WorkflowConfigurationResult(
            WorkflowConfigurationOutcome.Success,
            await GetAsync(cancellationToken));
    }

    public async Task<WorkflowConfigurationResult> ReplaceTransitionsAsync(
        ReplaceWorkflowTransitionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId, out var clinicId))
            return new WorkflowConfigurationResult(WorkflowConfigurationOutcome.Unauthenticated);

        var pairs = request.Transitions
            .Select(item => (item.FromStatusId, item.ToStatusId))
            .Distinct()
            .ToList();

        if (pairs.Count != request.Transitions.Count)
        {
            return new WorkflowConfigurationResult(
                WorkflowConfigurationOutcome.ValidationError,
                Error: "Duplicate workflow transitions are not allowed.");
        }

        var statusIds = pairs
            .SelectMany(pair => new[] { pair.FromStatusId, pair.ToStatusId })
            .Distinct()
            .ToArray();

        var existingStatusIds = statusIds.Length == 0
            ? Array.Empty<Guid>()
            : await dbContext.WorkflowStatuses.AsNoTracking()
                .Where(status => statusIds.Contains(status.Id))
                .Select(status => status.Id)
                .ToArrayAsync(cancellationToken);

        if (existingStatusIds.Length != statusIds.Length)
        {
            return new WorkflowConfigurationResult(
                WorkflowConfigurationOutcome.ValidationError,
                Error: "Every workflow transition must reference a valid status.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existing = await dbContext.WorkflowTransitions.ToListAsync(cancellationToken);
        dbContext.WorkflowTransitions.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var pair in pairs)
        {
            dbContext.WorkflowTransitions.Add(new WorkflowTransition
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                FromStatusId = pair.FromStatusId,
                ToStatusId = pair.ToStatusId,
                CreatedDate = now,
                CreateByUserId = userId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "WorkflowTransitions.Replaced",
            nameof(WorkflowTransition),
            metadata: new Dictionary<string, object?> { ["count"] = pairs.Count },
            cancellationToken: cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new WorkflowConfigurationResult(
            WorkflowConfigurationOutcome.Success,
            await GetAsync(cancellationToken));
    }

    private bool TryGetCurrentActor(out Guid userId, out Guid clinicId)
    {
        if (currentUserContext.IsAuthenticated &&
            currentUserContext.UserId is Guid currentUserId &&
            currentUserContext.ClinicId is Guid currentClinicId)
        {
            userId = currentUserId;
            clinicId = currentClinicId;
            return true;
        }

        userId = Guid.Empty;
        clinicId = Guid.Empty;
        return false;
    }

    private static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();
}
