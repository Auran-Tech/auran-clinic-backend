using System.Data;
using Microsoft.Data.SqlClient;
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
        QueueQuery options,
        CancellationToken cancellationToken = default)
    {
        var recentExitCutoff = DateTime.UtcNow - QueueListOptions.RecentExitWindow;
        var query =
            from queue in dbContext.QueueEntries.AsNoTracking()
            join patient in dbContext.Patients.AsNoTracking() on queue.PatientId equals patient.Id
            join status in dbContext.WorkflowStatuses.AsNoTracking() on queue.WorkflowStatusId equals status.Id
            join doctor in dbContext.Users.AsNoTracking() on queue.DoctorId equals doctor.Id into doctors
            from doctor in doctors.DefaultIfEmpty()
            where queue.ExitAtUtc == null ||
                (options.IncludeRecentExits && queue.ExitAtUtc >= recentExitCutoff)
            select new { queue, patient, status, doctor };

        var ordered = (options.SortBy, options.Descending) switch
        {
            (QueueListOptions.Arrival, true) => query.OrderByDescending(item => item.queue.EntryAtUtc),
            (QueueListOptions.Arrival, false) => query.OrderBy(item => item.queue.EntryAtUtc),
            (QueueListOptions.PatientName, true) => query.OrderByDescending(item => item.patient.FullName),
            (QueueListOptions.PatientName, false) => query.OrderBy(item => item.patient.FullName),
            (QueueListOptions.DoctorName, true) => query.OrderByDescending(item => item.doctor!.FullName),
            (QueueListOptions.DoctorName, false) => query.OrderBy(item => item.doctor!.FullName),
            (QueueListOptions.Stage, true) => query.OrderByDescending(item => item.status.SortOrder)
                .ThenByDescending(item => item.queue.EntryAtUtc),
            _ => query.OrderBy(item => item.status.SortOrder)
                .ThenBy(item => item.queue.EntryAtUtc)
        };

        return await ordered
            .ThenBy(item => item.queue.Id)
            .Select(item => new QueueEntryResponse(
                item.queue.Id,
                item.queue.VisitId,
                item.queue.PatientId,
                item.patient.PatientNumber,
                item.patient.FullName,
                item.queue.DoctorId,
                item.doctor != null ? item.doctor.FullName : null,
                item.status.Id,
                item.status.Code,
                item.status.Name,
                item.status.Color,
                item.queue.EntryAtUtc,
                item.queue.ExitAtUtc))
            .ToListAsync(cancellationToken);
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

        if (currentUserContext.ClinicId is not Guid clinicId)
            return new QueueMoveResult(QueueMoveOutcome.Unauthenticated);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        // Acquire the lock before checking the current stage. A concurrent move
        // must see the committed stage or exit, not a stale transition.
        var result = new SqlParameter("@lockResult", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255)
        {
            Value = $"auran:queue-move:{clinicId:N}:{request.QueueEntryId:N}"
        };
        await dbContext.Database.ExecuteSqlRawAsync(
            "EXEC @lockResult = sys.sp_getapplock @Resource = @resource, " +
            "@LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000",
            [result, resource],
            cancellationToken);
        if (result.Value is not int code || code < 0)
            throw new InvalidOperationException("Unable to acquire the queue transition lock.");

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
        await transaction.CommitAsync(cancellationToken);

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
