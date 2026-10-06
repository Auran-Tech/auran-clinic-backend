using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Queue;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Queue;

public sealed class QueueService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IQueueService
{
    public async Task<QueueBoardResponse> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await dbContext.WorkflowStatuses
            .AsNoTracking()
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Name)
            .Select(status => new QueueWorkflowStatusResponse(
                status.Id,
                status.Code,
                status.Name,
                status.Color,
                status.SortOrder,
                status.IsSystemFinal))
            .ToListAsync(cancellationToken);

        var rows = await (
                from queue in dbContext.QueueEntries.AsNoTracking()
                join patient in dbContext.Patients.AsNoTracking()
                    on queue.PatientId equals patient.Id
                join status in dbContext.WorkflowStatuses.AsNoTracking()
                    on queue.WorkflowStatusId equals status.Id
                join doctor in dbContext.Users.AsNoTracking()
                    on queue.DoctorId equals doctor.Id into doctorJoin
                from doctor in doctorJoin.DefaultIfEmpty()
                where queue.ExitAtUtc == null
                orderby status.SortOrder, queue.EntryAtUtc
                select new
                {
                    Queue = queue,
                    Patient = patient,
                    Status = status,
                    DoctorName = doctor == null ? null : doctor.FullName
                })
            .ToListAsync(cancellationToken);

        var staff = await (
                from user in dbContext.Users.AsNoTracking()
                join userRole in dbContext.UserRoles.AsNoTracking()
                    on user.Id equals userRole.UserId
                join role in dbContext.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where user.IsActive && role.Code == SystemRoleCatalog.Doctor
                orderby user.FullName
                select new QueueStaffResponse(user.Id, user.FullName))
            .Distinct()
            .ToListAsync(cancellationToken);

        var transitions = await dbContext.WorkflowTransitions
            .AsNoTracking()
            .Select(transition => new QueueTransitionResponse(
                transition.FromStatusId,
                transition.ToStatusId))
            .ToListAsync(cancellationToken);

        return new QueueBoardResponse(
            statuses,
            rows.Select(row => Map(
                row.Queue,
                row.Patient.PatientNumber,
                row.Patient.FullName,
                row.DoctorName,
                row.Status)).ToList(),
            staff,
            transitions);
    }

    public async Task<QueueMutationResult> CheckInAsync(
        QueueCheckInRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return new QueueMutationResult(QueueMutationOutcome.Unauthenticated);

        var patient = await dbContext.Patients
            .SingleOrDefaultAsync(item => item.Id == request.PatientId, cancellationToken);
        if (patient is null)
            return new QueueMutationResult(QueueMutationOutcome.NotFound, Error: "Patient not found.");

        var doctor = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == request.DoctorId && item.IsActive,
                cancellationToken);
        if (doctor is null)
            return new QueueMutationResult(QueueMutationOutcome.NotFound, Error: "Doctor not found or inactive.");

        var hasActiveQueue = await dbContext.QueueEntries
            .AsNoTracking()
            .AnyAsync(item => item.PatientId == request.PatientId && item.ExitAtUtc == null, cancellationToken);
        if (hasActiveQueue)
            return new QueueMutationResult(QueueMutationOutcome.Conflict, Error: "Patient already has an active queue entry.");

        var initialStatus = await dbContext.WorkflowStatuses
            .AsNoTracking()
            .Where(status => !status.IsSystemFinal)
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (initialStatus is null)
            return new QueueMutationResult(
                QueueMutationOutcome.ConfigurationRequired,
                Error: "Queue workflow is not configured.");

        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var visit = new Visit
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Status = VisitStatus.Open,
                DocumentationStatus = DocumentationStatus.NotStarted,
                EntryAtUtc = now,
                CreatedDate = now,
                CreateByUserId = userId
            };
            dbContext.Visits.Add(visit);

            var queueEntry = new QueueEntry
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                PatientId = patient.Id,
                VisitId = visit.Id,
                DoctorId = doctor.Id,
                WorkflowStatusId = initialStatus.Id,
                EntryAtUtc = now,
                CreatedDate = now,
                CreateByUserId = userId
            };
            dbContext.QueueEntries.Add(queueEntry);

            dbContext.QueueStatusHistory.Add(new QueueStatusHistory
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                QueueEntryId = queueEntry.Id,
                FromStatusId = null,
                ToStatusId = initialStatus.Id,
                ChangedAtUtc = now,
                ChangedByUserId = userId,
                CreatedDate = now,
                CreateByUserId = userId
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(
                "Queue.CheckedIn",
                nameof(QueueEntry),
                queueEntry.Id.ToString(),
                new Dictionary<string, object?>
                {
                    ["patientId"] = patient.Id,
                    ["visitId"] = visit.Id,
                    ["doctorId"] = doctor.Id,
                    ["statusCode"] = initialStatus.Code
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new QueueMutationResult(
                QueueMutationOutcome.Success,
                Map(queueEntry, patient.PatientNumber, patient.FullName, doctor.FullName, initialStatus));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<QueueMutationResult> MoveAsync(
        QueueMoveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return new QueueMutationResult(QueueMutationOutcome.Unauthenticated);

        var entry = await dbContext.QueueEntries
            .SingleOrDefaultAsync(item => item.Id == request.QueueEntryId && item.ExitAtUtc == null, cancellationToken);
        if (entry is null)
            return new QueueMutationResult(QueueMutationOutcome.NotFound, Error: "Queue entry not found.");

        byte[] expectedRowVersion;
        try
        {
            expectedRowVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            return new QueueMutationResult(QueueMutationOutcome.ValidationError, Error: "Invalid queue version.");
        }

        dbContext.Entry(entry).Property(item => item.RowVersion).OriginalValue = expectedRowVersion;

        var transitionAllowed = await dbContext.WorkflowTransitions
            .AsNoTracking()
            .AnyAsync(
                item => item.FromStatusId == entry.WorkflowStatusId && item.ToStatusId == request.ToStatusId,
                cancellationToken);
        if (!transitionAllowed)
            return new QueueMutationResult(QueueMutationOutcome.ValidationError, Error: "Workflow transition is not allowed.");

        var fromStatusId = entry.WorkflowStatusId;
        var targetStatus = await dbContext.WorkflowStatuses
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.ToStatusId, cancellationToken);
        if (targetStatus is null)
            return new QueueMutationResult(QueueMutationOutcome.NotFound, Error: "Workflow status not found.");

        var now = DateTime.UtcNow;
        entry.WorkflowStatusId = targetStatus.Id;
        entry.UpdatedDate = now;
        entry.UpdatedByUserId = userId;
        if (targetStatus.IsSystemFinal)
            entry.ExitAtUtc = now;

        dbContext.QueueStatusHistory.Add(new QueueStatusHistory
        {
            Id = Guid.NewGuid(),
            ClinicId = entry.ClinicId,
            QueueEntryId = entry.Id,
            FromStatusId = fromStatusId,
            ToStatusId = targetStatus.Id,
            ChangedAtUtc = now,
            ChangedByUserId = userId,
            Notes = Clean(request.Notes),
            CreatedDate = now,
            CreateByUserId = userId
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new QueueMutationResult(
                QueueMutationOutcome.Conflict,
                Error: "Queue entry changed by another user. Refresh the board and try again.");
        }

        var patient = await dbContext.Patients
            .AsNoTracking()
            .SingleAsync(item => item.Id == entry.PatientId, cancellationToken);
        var doctorName = entry.DoctorId.HasValue
            ? await dbContext.Users.AsNoTracking()
                .Where(item => item.Id == entry.DoctorId.Value)
                .Select(item => item.FullName)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        await auditService.WriteAsync(
            "Queue.StatusChanged",
            nameof(QueueEntry),
            entry.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["fromStatusId"] = fromStatusId,
                ["toStatusId"] = targetStatus.Id,
                ["toStatusCode"] = targetStatus.Code
            },
            cancellationToken);

        return new QueueMutationResult(
            QueueMutationOutcome.Success,
            Map(entry, patient.PatientNumber, patient.FullName, doctorName, targetStatus));
    }

    private static QueueEntryResponse Map(
        QueueEntry entry,
        string patientNumber,
        string patientName,
        string? doctorName,
        WorkflowStatus status) =>
        new(
            entry.Id,
            entry.PatientId,
            entry.VisitId,
            entry.DoctorId,
            patientNumber,
            patientName,
            doctorName,
            entry.WorkflowStatusId,
            status.Code,
            status.Name,
            status.Color,
            entry.EntryAtUtc,
            entry.ExitAtUtc,
            Convert.ToBase64String(entry.RowVersion));

    private bool TryGetActor(out Guid userId, out Guid clinicId)
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

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
