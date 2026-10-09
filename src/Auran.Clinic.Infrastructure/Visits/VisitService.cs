using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Visits;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Visits;

public sealed class VisitService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IVisitService
{
    public async Task<VisitStartResult> StartAsync(
        StartVisitRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId, out var clinicId))
            return new VisitStartResult(VisitStartOutcome.Unauthenticated);

        var patientExists = await dbContext.Patients.AsNoTracking()
            .AnyAsync(patient => patient.Id == request.PatientId, cancellationToken);
        if (!patientExists)
            return new VisitStartResult(VisitStartOutcome.PatientNotFound);

        var doctorExists = await dbContext.Users.AsNoTracking()
            .AnyAsync(user => user.Id == request.DoctorId && user.IsActive, cancellationToken);
        if (!doctorExists)
            return new VisitStartResult(VisitStartOutcome.DoctorNotFound);

        var activeVisitExists = await dbContext.Visits.AsNoTracking()
            .AnyAsync(
                visit => visit.PatientId == request.PatientId && visit.Status == VisitStatus.Open,
                cancellationToken);
        if (activeVisitExists)
            return new VisitStartResult(VisitStartOutcome.ActiveVisitExists);

        var initialStatus = await dbContext.WorkflowStatuses.AsNoTracking()
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (initialStatus is null)
            return new VisitStartResult(VisitStartOutcome.WorkflowNotConfigured);

        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var visit = new Visit
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                Status = VisitStatus.Open,
                DocumentationStatus = DocumentationStatus.NotStarted,
                EntryAtUtc = now,
                CreatedDate = now,
                CreateByUserId = userId
            };

            var queueEntry = new QueueEntry
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                PatientId = request.PatientId,
                VisitId = visit.Id,
                DoctorId = request.DoctorId,
                WorkflowStatusId = initialStatus.Id,
                EntryAtUtc = now,
                CreatedDate = now,
                CreateByUserId = userId
            };

            dbContext.Visits.Add(visit);
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
                "Visit.Started",
                nameof(Visit),
                visit.Id.ToString(),
                new Dictionary<string, object?>
                {
                    ["patientId"] = visit.PatientId,
                    ["doctorId"] = visit.DoctorId,
                    ["workflowStatus"] = initialStatus.Code
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new VisitStartResult(
                VisitStartOutcome.Success,
                Map(visit, queueEntry, initialStatus));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<VisitResponse?> GetActiveForPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from visit in dbContext.Visits.AsNoTracking()
            join queueEntry in dbContext.QueueEntries.AsNoTracking()
                on visit.Id equals queueEntry.VisitId
            join workflowStatus in dbContext.WorkflowStatuses.AsNoTracking()
                on queueEntry.WorkflowStatusId equals workflowStatus.Id
            where visit.PatientId == patientId && visit.Status == VisitStatus.Open
            select new VisitResponse(
                visit.Id,
                visit.PatientId,
                visit.DoctorId,
                visit.Status.ToString(),
                visit.EntryAtUtc,
                queueEntry.Id,
                workflowStatus.Id,
                workflowStatus.Code,
                workflowStatus.Name);

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PatientVisitHistoryResponse?> ListForPatientAsync(
        PatientVisitHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await dbContext.Patients.AsNoTracking()
            .AnyAsync(patient => patient.Id == query.PatientId, cancellationToken);

        if (!patientExists)
            return null;

        var baseQuery =
            from visit in dbContext.Visits.AsNoTracking()
            join doctor in dbContext.Users.AsNoTracking()
                on visit.DoctorId equals doctor.Id
            where visit.PatientId == query.PatientId
            select new
            {
                Visit = visit,
                DoctorName = doctor.FullName,
                SessionCount = dbContext.VisitSessions.Count(
                    session => session.VisitId == visit.Id)
            };

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderByDescending(item => item.Visit.EntryAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new PatientVisitHistoryItemResponse(
                item.Visit.Id,
                item.Visit.DoctorId,
                item.DoctorName,
                item.Visit.Status.ToString(),
                item.Visit.DocumentationStatus.ToString(),
                item.Visit.EntryAtUtc,
                item.Visit.CompletedAtUtc,
                item.Visit.ExitAtUtc,
                item.Visit.ChiefComplaint,
                item.Visit.Diagnosis,
                item.SessionCount))
            .ToListAsync(cancellationToken);

        return new PatientVisitHistoryResponse(
            query.PatientId,
            new PaginatedResponse<PatientVisitHistoryItemResponse>
            {
                Data = items,
                Setting = new PaginationInfo
                {
                    TotalCount = totalCount,
                    RowCount = query.PageSize,
                    CurrentPage = query.Page
                }
            });
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

    private static VisitResponse Map(
        Visit visit,
        QueueEntry queueEntry,
        WorkflowStatus workflowStatus) =>
        new(
            visit.Id,
            visit.PatientId,
            visit.DoctorId,
            visit.Status.ToString(),
            visit.EntryAtUtc,
            queueEntry.Id,
            workflowStatus.Id,
            workflowStatus.Code,
            workflowStatus.Name);
}
