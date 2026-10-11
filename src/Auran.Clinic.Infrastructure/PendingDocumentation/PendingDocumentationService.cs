using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.PendingDocumentation;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.PendingDocumentation;

public sealed class PendingDocumentationService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IPendingDocumentationService
{
    public async Task<IReadOnlyList<PendingDocumentationResponse>> ListAsync(
        PendingDocumentationQuery query,
        CancellationToken cancellationToken = default)
    {
        var visits = dbContext.Visits.AsNoTracking()
            .Where(visit =>
                visit.DocumentationStatus == DocumentationStatus.Draft ||
                visit.DocumentationStatus == DocumentationStatus.Pending);

        if (query.MineOnly &&
            currentUserContext.IsAuthenticated &&
            currentUserContext.UserId is Guid userId &&
            !currentUserContext.IsSuperUser)
        {
            visits = visits.Where(visit => visit.DoctorId == userId);
        }

        return await (
            from visit in visits
            join patient in dbContext.Patients.AsNoTracking() on visit.PatientId equals patient.Id
            join doctor in dbContext.Users.AsNoTracking() on visit.DoctorId equals doctor.Id
            orderby visit.EntryAtUtc
            select new PendingDocumentationResponse(
                visit.Id,
                patient.Id,
                patient.PatientNumber,
                patient.FullName,
                doctor.Id,
                doctor.FullName,
                visit.Status.ToString(),
                visit.DocumentationStatus.ToString(),
                visit.EntryAtUtc,
                visit.CompletedAtUtc,
                visit.ExitAtUtc,
                visit.ChiefComplaint,
                visit.Examination,
                visit.Diagnosis,
                visit.Notes,
                visit.TreatmentPlan))
            .ToListAsync(cancellationToken);
    }

    public async Task<PendingDocumentationResult> CompleteAsync(
        CompletePendingDocumentationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated || currentUserContext.UserId is not Guid userId)
            return new PendingDocumentationResult(PendingDocumentationOutcome.Unauthenticated);

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);
        if (visit is null)
            return new PendingDocumentationResult(PendingDocumentationOutcome.VisitNotFound);

        if (visit.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new PendingDocumentationResult(PendingDocumentationOutcome.Forbidden);

        if (visit.DocumentationStatus is not (DocumentationStatus.Draft or DocumentationStatus.Pending))
            return new PendingDocumentationResult(PendingDocumentationOutcome.NotPending);

        visit.ChiefComplaint = Clean(request.ChiefComplaint);
        visit.Examination = Clean(request.Examination);
        visit.Diagnosis = Clean(request.Diagnosis);
        visit.Notes = Clean(request.Notes);
        visit.TreatmentPlan = Clean(request.TreatmentPlan);
        visit.DocumentationStatus = DocumentationStatus.Completed;
        visit.UpdatedDate = DateTime.UtcNow;
        visit.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalDocumentation.Completed",
            nameof(Domain.Entities.Visit),
            visit.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = visit.PatientId,
                ["doctorId"] = visit.DoctorId
            },
            cancellationToken);

        var response = await (
            from currentVisit in dbContext.Visits.AsNoTracking()
            join patient in dbContext.Patients.AsNoTracking() on currentVisit.PatientId equals patient.Id
            join doctor in dbContext.Users.AsNoTracking() on currentVisit.DoctorId equals doctor.Id
            where currentVisit.Id == visit.Id
            select new PendingDocumentationResponse(
                currentVisit.Id,
                patient.Id,
                patient.PatientNumber,
                patient.FullName,
                doctor.Id,
                doctor.FullName,
                currentVisit.Status.ToString(),
                currentVisit.DocumentationStatus.ToString(),
                currentVisit.EntryAtUtc,
                currentVisit.CompletedAtUtc,
                currentVisit.ExitAtUtc,
                currentVisit.ChiefComplaint,
                currentVisit.Examination,
                currentVisit.Diagnosis,
                currentVisit.Notes,
                currentVisit.TreatmentPlan))
            .SingleAsync(cancellationToken);

        return new PendingDocumentationResult(
            PendingDocumentationOutcome.Success,
            response);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
