using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.ClinicalSessions;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.ClinicalSessions;

public sealed class ClinicalSessionService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IClinicalSessionService
{
    public async Task<ClinicalSessionResponse?> GetActiveAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from session in dbContext.VisitSessions.AsNoTracking()
            join visit in dbContext.Visits.AsNoTracking() on session.VisitId equals visit.Id
            where session.VisitId == visitId && session.EndedAtUtc == null
            select new ClinicalSessionResponse(
                session.Id,
                session.VisitId,
                session.DoctorId,
                session.StartedAtUtc,
                session.EndedAtUtc,
                visit.DocumentationStatus.ToString(),
                visit.ChiefComplaint,
                visit.Examination,
                visit.Diagnosis,
                visit.Notes,
                visit.TreatmentPlan))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ClinicalSessionResult> StartAsync(
        StartClinicalSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId))
            return new ClinicalSessionResult(ClinicalSessionOutcome.Unauthenticated);

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);
        if (visit is null)
            return new ClinicalSessionResult(ClinicalSessionOutcome.VisitNotFound);
        if (visit.Status != VisitStatus.Open)
            return new ClinicalSessionResult(ClinicalSessionOutcome.VisitClosed);
        if (visit.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new ClinicalSessionResult(ClinicalSessionOutcome.Forbidden);

        var activeExists = await dbContext.VisitSessions.AsNoTracking()
            .AnyAsync(item => item.VisitId == visit.Id && item.EndedAtUtc == null, cancellationToken);
        if (activeExists)
            return new ClinicalSessionResult(ClinicalSessionOutcome.SessionAlreadyActive);

        var now = DateTime.UtcNow;
        var session = new VisitSession
        {
            Id = Guid.NewGuid(),
            ClinicId = visit.ClinicId,
            VisitId = visit.Id,
            DoctorId = visit.DoctorId,
            StartedAtUtc = now,
            CreatedByUserId = userId,
            CreatedDate = now,
            CreateByUserId = userId
        };

        visit.DocumentationStatus = DocumentationStatus.Draft;
        visit.UpdatedDate = now;
        visit.UpdatedByUserId = userId;

        dbContext.VisitSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalSession.Started",
            nameof(VisitSession),
            session.Id.ToString(),
            new Dictionary<string, object?> { ["visitId"] = visit.Id },
            cancellationToken);

        return new ClinicalSessionResult(
            ClinicalSessionOutcome.Success,
            Map(session, visit));
    }

    public async Task<ClinicalSessionResult> SaveDocumentationAsync(
        SaveClinicalDocumentationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId))
            return new ClinicalSessionResult(ClinicalSessionOutcome.Unauthenticated);

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);
        if (visit is null)
            return new ClinicalSessionResult(ClinicalSessionOutcome.VisitNotFound);
        if (visit.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new ClinicalSessionResult(ClinicalSessionOutcome.Forbidden);

        var session = await dbContext.VisitSessions
            .SingleOrDefaultAsync(
                item => item.VisitId == visit.Id && item.EndedAtUtc == null,
                cancellationToken);
        if (session is null)
            return new ClinicalSessionResult(ClinicalSessionOutcome.SessionNotFound);

        visit.ChiefComplaint = Clean(request.ChiefComplaint);
        visit.Examination = Clean(request.Examination);
        visit.Diagnosis = Clean(request.Diagnosis);
        visit.Notes = Clean(request.Notes);
        visit.TreatmentPlan = Clean(request.TreatmentPlan);
        visit.DocumentationStatus = DocumentationStatus.Draft;
        visit.UpdatedDate = DateTime.UtcNow;
        visit.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalDocumentation.Saved",
            nameof(Visit),
            visit.Id.ToString(),
            cancellationToken: cancellationToken);

        return new ClinicalSessionResult(
            ClinicalSessionOutcome.Success,
            Map(session, visit));
    }

    public async Task<ClinicalSessionResult> EndAsync(
        EndClinicalSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId))
            return new ClinicalSessionResult(ClinicalSessionOutcome.Unauthenticated);

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);
        if (visit is null)
            return new ClinicalSessionResult(ClinicalSessionOutcome.VisitNotFound);
        if (visit.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new ClinicalSessionResult(ClinicalSessionOutcome.Forbidden);

        var session = await dbContext.VisitSessions
            .SingleOrDefaultAsync(
                item => item.VisitId == visit.Id && item.EndedAtUtc == null,
                cancellationToken);
        if (session is null)
            return new ClinicalSessionResult(ClinicalSessionOutcome.SessionNotFound);

        var now = DateTime.UtcNow;
        session.EndedAtUtc = now;
        session.UpdatedDate = now;
        session.UpdatedByUserId = userId;

        visit.DocumentationStatus = HasDocumentation(visit)
            ? DocumentationStatus.Completed
            : DocumentationStatus.Pending;
        visit.UpdatedDate = now;
        visit.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalSession.Ended",
            nameof(VisitSession),
            session.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["visitId"] = visit.Id,
                ["documentationStatus"] = visit.DocumentationStatus.ToString()
            },
            cancellationToken);

        return new ClinicalSessionResult(
            ClinicalSessionOutcome.Success,
            Map(session, visit));
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

    private static ClinicalSessionResponse Map(VisitSession session, Visit visit) =>
        new(
            session.Id,
            session.VisitId,
            session.DoctorId,
            session.StartedAtUtc,
            session.EndedAtUtc,
            visit.DocumentationStatus.ToString(),
            visit.ChiefComplaint,
            visit.Examination,
            visit.Diagnosis,
            visit.Notes,
            visit.TreatmentPlan);

    private static bool HasDocumentation(Visit visit) =>
        !string.IsNullOrWhiteSpace(visit.ChiefComplaint) ||
        !string.IsNullOrWhiteSpace(visit.Examination) ||
        !string.IsNullOrWhiteSpace(visit.Diagnosis) ||
        !string.IsNullOrWhiteSpace(visit.Notes) ||
        !string.IsNullOrWhiteSpace(visit.TreatmentPlan);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
