using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Queue;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Queue;

public sealed class QueueService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IQueueService
{
    public async Task<IReadOnlyList<QueueEntryResponse>> ListActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var query =
            from queue in dbContext.QueueEntries.AsNoTracking()
            join patient in dbContext.Patients.AsNoTracking() on queue.PatientId equals patient.Id
            join status in dbContext.WorkflowStatuses.AsNoTracking() on queue.WorkflowStatusId equals status.Id
            join doctor in dbContext.Users.AsNoTracking() on queue.DoctorId equals doctor.Id into doctors
            from doctor in doctors.DefaultIfEmpty()
            where queue.ExitAtUtc == null
            orderby status.SortOrder, queue.EntryAtUtc
            select new QueueEntryResponse(
                queue.Id,
                queue.VisitId,
                queue.PatientId,
                patient.PatientNumber,
                patient.FullName,
                queue.DoctorId,
                doctor != null ? doctor.FullName : null,
                status.Id,
                status.Code,
                status.Name,
                status.Color,
                queue.EntryAtUtc,
                queue.ExitAtUtc);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QueueTransitionOptionResponse>> GetAvailableTransitionsAsync(
        Guid queueEntryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.QueueEntries.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == queueEntryId, cancellationToken);
        if (entry is null || entry.ExitAtUtc != null)
            return Array.Empty<QueueTransitionOptionResponse>();

        return await (
            from transition in dbContext.WorkflowTransitions.AsNoTracking()
            join status in dbContext.WorkflowStatuses.AsNoTracking()
                on transition.ToStatusId equals status.Id
            where transition.FromStatusId == entry.WorkflowStatusId
            orderby status.SortOrder, status.Name
            select new QueueTransitionOptionResponse(
                status.Id,
                status.Code,
                status.Name,
                status.Color,
                status.IsSystemFinal))
            .ToListAsync(cancellationToken);
    }

    public async Task<QueueMoveResult> MoveAsync(
        MoveQueueEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId))
            return new QueueMoveResult(QueueMoveOutcome.Unauthenticated);

        var entry = await dbContext.QueueEntries
            .SingleOrDefaultAsync(item => item.Id == request.QueueEntryId, cancellationToken);
        if (entry is null)
            return new QueueMoveResult(QueueMoveOutcome.NotFound);
        if (entry.ExitAtUtc != null)
            return new QueueMoveResult(QueueMoveOutcome.AlreadyExited);

        var transition = await dbContext.WorkflowTransitions.AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.FromStatusId == entry.WorkflowStatusId &&
                    item.ToStatusId == request.ToWorkflowStatusId,
                cancellationToken);
        if (transition is null)
            return new QueueMoveResult(QueueMoveOutcome.InvalidTransition);

        var targetStatus = await dbContext.WorkflowStatuses
            .SingleOrDefaultAsync(status => status.Id == request.ToWorkflowStatusId, cancellationToken);
        if (targetStatus is null)
            return new QueueMoveResult(QueueMoveOutcome.InvalidTransition);

        var fromStatusId = entry.WorkflowStatusId;
        var now = DateTime.UtcNow;

        entry.WorkflowStatusId = targetStatus.Id;
        entry.UpdatedDate = now;
        entry.UpdatedByUserId = userId;

        if (targetStatus.IsSystemFinal)
        {
            entry.ExitAtUtc = now;

            var visit = await dbContext.Visits
                .SingleAsync(item => item.Id == entry.VisitId, cancellationToken);
            visit.ExitAtUtc = now;
            visit.CompletedAtUtc ??= now;
            visit.Status = Domain.Enums.VisitStatus.Completed;
            visit.UpdatedDate = now;
            visit.UpdatedByUserId = userId;
        }

        dbContext.QueueStatusHistory.Add(new Domain.Entities.QueueStatusHistory
        {
            Id = Guid.NewGuid(),
            ClinicId = entry.ClinicId,
            QueueEntryId = entry.Id,
            FromStatusId = fromStatusId,
            ToStatusId = targetStatus.Id,
            ChangedAtUtc = now,
            ChangedByUserId = userId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedDate = now,
            CreateByUserId = userId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            "Queue.StatusChanged",
            nameof(Domain.Entities.QueueEntry),
            entry.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["fromStatusId"] = fromStatusId,
                ["toStatusId"] = targetStatus.Id,
                ["isFinal"] = targetStatus.IsSystemFinal
            },
            cancellationToken);

        var response = await (
            from queue in dbContext.QueueEntries.AsNoTracking()
            join patient in dbContext.Patients.AsNoTracking() on queue.PatientId equals patient.Id
            join status in dbContext.WorkflowStatuses.AsNoTracking() on queue.WorkflowStatusId equals status.Id
            join doctor in dbContext.Users.AsNoTracking() on queue.DoctorId equals doctor.Id into doctors
            from doctor in doctors.DefaultIfEmpty()
            where queue.Id == entry.Id
            select new QueueEntryResponse(
                queue.Id,
                queue.VisitId,
                queue.PatientId,
                patient.PatientNumber,
                patient.FullName,
                queue.DoctorId,
                doctor != null ? doctor.FullName : null,
                status.Id,
                status.Code,
                status.Name,
                status.Color,
                queue.EntryAtUtc,
                queue.ExitAtUtc))
            .SingleAsync(cancellationToken);

        return new QueueMoveResult(QueueMoveOutcome.Success, response);
    }

    private bool TryGetCurrentActor(out Guid userId)
    {
        if (currentUserContext.IsAuthenticated && currentUserContext.UserId is Guid currentUserId)
        {
            userId = currentUserId;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }
}
