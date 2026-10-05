using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authorization;
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
    public async Task<IReadOnlyCollection<VisitSummaryResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
                from visit in dbContext.Visits.AsNoTracking()
                join patient in dbContext.Patients.AsNoTracking()
                    on visit.PatientId equals patient.Id
                join doctor in dbContext.Users.AsNoTracking()
                    on visit.DoctorId equals doctor.Id
                orderby visit.EntryAtUtc descending
                select new VisitSummaryResponse(
                    visit.Id,
                    patient.Id,
                    patient.PatientNumber,
                    patient.FullName,
                    doctor.Id,
                    doctor.FullName,
                    visit.Status,
                    visit.DocumentationStatus,
                    visit.EntryAtUtc,
                    visit.CompletedAtUtc,
                    Convert.ToBase64String(visit.RowVersion)))
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<VisitDetailsResponse?> GetAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        var visitRow = await (
                from visit in dbContext.Visits.AsNoTracking()
                join patient in dbContext.Patients.AsNoTracking()
                    on visit.PatientId equals patient.Id
                join doctor in dbContext.Users.AsNoTracking()
                    on visit.DoctorId equals doctor.Id
                where visit.Id == visitId
                select new
                {
                    Visit = visit,
                    PatientNumber = patient.PatientNumber,
                    PatientName = patient.FullName,
                    DoctorName = doctor.FullName
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (visitRow is null)
            return null;

        var sessions = await (
                from session in dbContext.VisitSessions.AsNoTracking()
                join doctor in dbContext.Users.AsNoTracking()
                    on session.DoctorId equals doctor.Id
                where session.VisitId == visitId
                orderby session.StartedAtUtc
                select new VisitSessionResponse(
                    session.Id,
                    session.DoctorId,
                    doctor.FullName,
                    session.StartedAtUtc,
                    session.EndedAtUtc))
            .ToListAsync(cancellationToken);

        var availableDoctors = await (
                from user in dbContext.Users.AsNoTracking()
                join userRole in dbContext.UserRoles.AsNoTracking()
                    on user.Id equals userRole.UserId
                join role in dbContext.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where user.IsActive && role.Code == SystemRoleCatalog.Doctor
                orderby user.FullName
                select new VisitDoctorResponse(user.Id, user.FullName))
            .Distinct()
            .ToListAsync(cancellationToken);

        return new VisitDetailsResponse(
            new VisitSummaryResponse(
                visitRow.Visit.Id,
                visitRow.Visit.PatientId,
                visitRow.PatientNumber,
                visitRow.PatientName,
                visitRow.Visit.DoctorId,
                visitRow.DoctorName,
                visitRow.Visit.Status,
                visitRow.Visit.DocumentationStatus,
                visitRow.Visit.EntryAtUtc,
                visitRow.Visit.CompletedAtUtc,
                Convert.ToBase64String(visitRow.Visit.RowVersion)),
            visitRow.Visit.ChiefComplaint,
            visitRow.Visit.Examination,
            visitRow.Visit.Diagnosis,
            visitRow.Visit.Notes,
            visitRow.Visit.TreatmentPlan,
            sessions,
            availableDoctors);
    }

    public async Task<VisitMutationResult> StartSessionAsync(
        StartVisitSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return new VisitMutationResult(VisitMutationOutcome.Unauthenticated);

        var visit = await dbContext.Visits
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);
        if (visit is null)
            return new VisitMutationResult(VisitMutationOutcome.NotFound, Error: "Visit not found.");
        if (visit.Status != VisitStatus.Open)
            return new VisitMutationResult(VisitMutationOutcome.ValidationError, Error: "Only open visits can start a session.");

        var doctorExists = await (
                from user in dbContext.Users.AsNoTracking()
                join userRole in dbContext.UserRoles.AsNoTracking()
                    on user.Id equals userRole.UserId
                join role in dbContext.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where user.Id == request.DoctorId
                      && user.IsActive
                      && role.Code == SystemRoleCatalog.Doctor
                select user.Id)
            .AnyAsync(cancellationToken);

        if (!doctorExists)
            return new VisitMutationResult(VisitMutationOutcome.NotFound, Error: "Doctor not found or inactive.");

        var activeSessionExists = await dbContext.VisitSessions
            .AsNoTracking()
            .AnyAsync(item => item.VisitId == request.VisitId && item.EndedAtUtc == null, cancellationToken);
        if (activeSessionExists)
            return new VisitMutationResult(VisitMutationOutcome.Conflict, Error: "Visit already has an active session.");

        var now = DateTime.UtcNow;
        var session = new VisitSession
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            VisitId = request.VisitId,
            DoctorId = request.DoctorId,
            StartedAtUtc = now,
            CreatedByUserId = userId,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.VisitSessions.Add(session);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new VisitMutationResult(VisitMutationOutcome.Conflict, Error: "Visit already has an active session.");
        }

        await auditService.WriteAsync(
            "Visit.SessionStarted",
            nameof(VisitSession),
            session.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["visitId"] = request.VisitId,
                ["doctorId"] = request.DoctorId
            },
            cancellationToken);

        return new VisitMutationResult(
            VisitMutationOutcome.Success,
            await GetAsync(request.VisitId, cancellationToken));
    }

    public async Task<VisitMutationResult> EndSessionAsync(
        EndVisitSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return new VisitMutationResult(VisitMutationOutcome.Unauthenticated);

        var session = await dbContext.VisitSessions
            .SingleOrDefaultAsync(
                item => item.Id == request.SessionId
                        && item.VisitId == request.VisitId
                        && item.EndedAtUtc == null,
                cancellationToken);

        if (session is null)
            return new VisitMutationResult(VisitMutationOutcome.NotFound, Error: "Active visit session not found.");

        var now = DateTime.UtcNow;
        session.EndedAtUtc = now;
        session.UpdatedDate = now;
        session.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "Visit.SessionEnded",
            nameof(VisitSession),
            session.Id.ToString(),
            new Dictionary<string, object?> { ["visitId"] = request.VisitId },
            cancellationToken);

        return new VisitMutationResult(
            VisitMutationOutcome.Success,
            await GetAsync(request.VisitId, cancellationToken));
    }

    public async Task<VisitMutationResult> SaveDraftAsync(
        SaveVisitDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return new VisitMutationResult(VisitMutationOutcome.Unauthenticated);

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);
        if (visit is null)
            return new VisitMutationResult(VisitMutationOutcome.NotFound, Error: "Visit not found.");
        if (visit.Status != VisitStatus.Open)
            return new VisitMutationResult(VisitMutationOutcome.ValidationError, Error: "Completed visits cannot be edited as drafts.");

        byte[] expectedVersion;
        try
        {
            expectedVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            return new VisitMutationResult(VisitMutationOutcome.ValidationError, Error: "Invalid visit version.");
        }

        dbContext.Entry(visit).Property(item => item.RowVersion).OriginalValue = expectedVersion;

        visit.ChiefComplaint = Clean(request.ChiefComplaint);
        visit.Examination = Clean(request.Examination);
        visit.Diagnosis = Clean(request.Diagnosis);
        visit.Notes = Clean(request.Notes);
        visit.TreatmentPlan = Clean(request.TreatmentPlan);
        visit.DocumentationStatus = DocumentationStatus.Draft;
        visit.UpdatedDate = DateTime.UtcNow;
        visit.UpdatedByUserId = userId;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new VisitMutationResult(
                VisitMutationOutcome.Conflict,
                Error: "Visit documentation changed by another user. Reload the visit before saving.");
        }

        await auditService.WriteAsync(
            "Visit.DraftSaved",
            nameof(Visit),
            visit.Id.ToString(),
            new Dictionary<string, object?> { ["documentationStatus"] = visit.DocumentationStatus.ToString() },
            cancellationToken);

        return new VisitMutationResult(
            VisitMutationOutcome.Success,
            await GetAsync(request.VisitId, cancellationToken));
    }

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
