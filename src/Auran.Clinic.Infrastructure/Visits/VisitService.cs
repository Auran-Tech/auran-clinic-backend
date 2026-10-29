using System.Data;
using Microsoft.Data.SqlClient;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authorization;
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
    public async Task<IReadOnlyList<VisitDoctorOptionResponse>> ListVisitDoctorsAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
                from visit in dbContext.Visits.AsNoTracking()
                join doctor in dbContext.Users.AsNoTracking()
                    on visit.DoctorId equals doctor.Id
                select new { doctor.Id, doctor.FullName })
            .Distinct()
            .OrderBy(doctor => doctor.FullName)
            .ThenBy(doctor => doctor.Id)
            .Select(doctor => new VisitDoctorOptionResponse(doctor.Id, doctor.FullName))
            .ToListAsync(cancellationToken);
    }

    public async Task<VisitListResponse> ListAsync(
        VisitListQuery query,
        CancellationToken cancellationToken = default)
    {
        var visits =
            from visit in dbContext.Visits.AsNoTracking()
            join patient in dbContext.Patients.AsNoTracking()
                on visit.PatientId equals patient.Id
            join doctor in dbContext.Users.AsNoTracking()
                on visit.DoctorId equals doctor.Id
            select new
            {
                Visit = visit,
                PatientName = patient.FullName,
                patient.PatientNumber,
                DoctorName = doctor.FullName
            };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var isVisitId = Guid.TryParse(search, out var visitId);
            visits = visits.Where(item =>
                (isVisitId && item.Visit.Id == visitId) ||
                item.PatientName.Contains(search) ||
                item.PatientNumber.Contains(search));
        }

        if (query.DoctorId.HasValue)
            visits = visits.Where(item => item.Visit.DoctorId == query.DoctorId.Value);
        if (query.Status.HasValue)
            visits = visits.Where(item => item.Visit.Status == query.Status.Value);
        if (query.DocumentationStatus.HasValue)
            visits = visits.Where(item =>
                item.Visit.DocumentationStatus == query.DocumentationStatus.Value);
        if (query.FromUtc.HasValue)
            visits = visits.Where(item => item.Visit.EntryAtUtc >= query.FromUtc.Value);
        if (query.ToUtc.HasValue)
            visits = visits.Where(item => item.Visit.EntryAtUtc <= query.ToUtc.Value);

        var totalCount = await visits.CountAsync(cancellationToken);

        var sortBy = query.SortBy.ToLowerInvariant();
        visits = (sortBy, query.Descending) switch
        {
            ("patientname", false) => visits.OrderBy(item => item.PatientName)
                .ThenBy(item => item.Visit.Id),
            ("patientname", true) => visits.OrderByDescending(item => item.PatientName)
                .ThenBy(item => item.Visit.Id),
            ("doctorname", false) => visits.OrderBy(item => item.DoctorName)
                .ThenBy(item => item.Visit.Id),
            ("doctorname", true) => visits.OrderByDescending(item => item.DoctorName)
                .ThenBy(item => item.Visit.Id),
            ("status", false) => visits.OrderBy(item => item.Visit.Status)
                .ThenBy(item => item.Visit.Id),
            ("status", true) => visits.OrderByDescending(item => item.Visit.Status)
                .ThenBy(item => item.Visit.Id),
            (_, false) => visits.OrderBy(item => item.Visit.EntryAtUtc)
                .ThenBy(item => item.Visit.Id),
            _ => visits.OrderByDescending(item => item.Visit.EntryAtUtc)
                .ThenByDescending(item => item.Visit.Id)
        };

        var items = await visits
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new VisitListItemResponse(
                item.Visit.Id,
                item.Visit.PatientId,
                item.PatientNumber,
                item.PatientName,
                item.Visit.DoctorId,
                item.DoctorName,
                item.Visit.Status.ToString(),
                item.Visit.DocumentationStatus.ToString(),
                item.Visit.EntryAtUtc,
                item.Visit.CompletedAtUtc,
                item.Visit.ExitAtUtc))
            .ToListAsync(cancellationToken);

        var active = await dbContext.Visits.AsNoTracking()
            .CountAsync(visit => visit.Status == VisitStatus.Open, cancellationToken);
        var completed = await dbContext.Visits.AsNoTracking()
            .CountAsync(visit => visit.Status == VisitStatus.Completed, cancellationToken);
        var pendingDocumentation = await dbContext.Visits.AsNoTracking()
            .CountAsync(visit =>
                visit.DocumentationStatus == DocumentationStatus.Draft ||
                visit.DocumentationStatus == DocumentationStatus.Pending,
                cancellationToken);

        return new VisitListResponse(new PaginatedResponse<VisitListItemResponse>
        {
            Data = items,
            Setting = new PaginationInfo
            {
                TotalCount = totalCount,
                RowCount = query.PageSize,
                CurrentPage = query.Page
            }
        }, new VisitListSummaryResponse(active, completed, pendingDocumentation));
    }

    public async Task<IReadOnlyList<VisitDoctorOptionResponse>> ListAvailableDoctorsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users.AsNoTracking()
            .Where(user => user.IsActive && dbContext.UserRoles.Any(userRole =>
                userRole.UserId == user.Id && dbContext.Roles.Any(role =>
                    role.Id == userRole.RoleId && role.Code == SystemRoleCatalog.Doctor)))
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Id)
            .Select(user => new VisitDoctorOptionResponse(user.Id, user.FullName))
            .ToListAsync(cancellationToken);
    }

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

        var initialStatus = await dbContext.WorkflowStatuses.AsNoTracking()
            .Where(status => !status.IsSystemFinal)
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (initialStatus is null)
            return new VisitStartResult(VisitStartOutcome.WorkflowNotConfigured);

        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Serialize check-ins for this clinic/patient across all API instances.
            // The transaction-owned SQL application lock is released on commit/rollback.
            var resource = $"auran:visit-start:{clinicId:N}:{request.PatientId:N}";
            var result = new SqlParameter("@lockResult", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            var resourceParameter = new SqlParameter("@resource", SqlDbType.NVarChar, 255)
            {
                Value = resource
            };
            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC @lockResult = sys.sp_getapplock @Resource = @resource, " +
                "@LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000",
                [result, resourceParameter],
                cancellationToken);

            if (result.Value is not int lockResult || lockResult < 0)
                throw new InvalidOperationException("Unable to acquire the patient visit-start lock.");

            // This check must be inside the transaction AFTER acquiring the lock.
            var activeVisitExists = await dbContext.Visits.AsNoTracking()
                .AnyAsync(
                    visit => visit.PatientId == request.PatientId && visit.Status == VisitStatus.Open,
                    cancellationToken);
            if (activeVisitExists)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new VisitStartResult(VisitStartOutcome.ActiveVisitExists);
            }

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
            .ThenByDescending(item => item.Visit.Id)
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
