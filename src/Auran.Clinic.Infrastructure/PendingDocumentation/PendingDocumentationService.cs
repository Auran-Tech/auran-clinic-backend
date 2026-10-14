using System.Data;
using Microsoft.Data.SqlClient;
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

        // A normal doctor must never bypass their assignment by supplying mineOnly=false.
        if (!currentUserContext.IsSuperUser)
        {
            if (!currentUserContext.IsAuthenticated || currentUserContext.UserId is not Guid userId)
                return Array.Empty<PendingDocumentationResponse>();
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

        if (currentUserContext.ClinicId is not Guid clinicId)
            return new PendingDocumentationResult(PendingDocumentationOutcome.Unauthenticated);

        // Serialize finalization before reading the mutable documentation state.
        // Competing requests must observe the first committed completion.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var lockResult = new SqlParameter("@lockResult", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255)
        {
            Value = $"auran:clinical-documentation-complete:{clinicId:N}:{request.VisitId:N}"
        };
        await dbContext.Database.ExecuteSqlRawAsync(
            "EXEC @lockResult = sys.sp_getapplock @Resource = @resource, " +
            "@LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000",
            [lockResult, resource],
            cancellationToken);
        if (lockResult.Value is not int code || code < 0)
            throw new InvalidOperationException("Unable to acquire clinical documentation finalization lock.");

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);
        if (visit is null)
            return new PendingDocumentationResult(PendingDocumentationOutcome.VisitNotFound);

        if (visit.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new PendingDocumentationResult(PendingDocumentationOutcome.Forbidden);

        if (visit.DocumentationStatus is not (DocumentationStatus.Draft or DocumentationStatus.Pending))
            return new PendingDocumentationResult(PendingDocumentationOutcome.NotPending);

        // Resolve the clinic-owned policy through the visit's assigned doctor.
        // Unconfigured specialties retain the legacy at-least-one-field validation.
        var requiredFields = await dbContext.Database.SqlQuery<string>(
            $"""
            SELECT p.RequiredFields AS Value
            FROM dbo.DoctorDocumentationSpecialties AS d
            INNER JOIN dbo.ClinicDocumentationSpecialtyPolicies AS p
              ON p.ClinicId = d.ClinicId AND p.SpecialtyCode = d.SpecialtyCode
            WHERE d.ClinicId = {clinicId} AND d.DoctorId = {visit.DoctorId}
            """).SingleOrDefaultAsync(cancellationToken);

        if (requiredFields is not null)
        {
            var missing = requiredFields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Where(field => field switch
                {
                    "ChiefComplaint" => string.IsNullOrWhiteSpace(request.ChiefComplaint),
                    "Examination" => string.IsNullOrWhiteSpace(request.Examination),
                    "Diagnosis" => string.IsNullOrWhiteSpace(request.Diagnosis),
                    "Notes" => string.IsNullOrWhiteSpace(request.Notes),
                    "TreatmentPlan" => string.IsNullOrWhiteSpace(request.TreatmentPlan),
                    _ => true
                })
                .ToArray();
            if (missing.Length > 0)
                return new PendingDocumentationResult(
                    PendingDocumentationOutcome.MissingRequiredFields, MissingFields: missing);
        }

        visit.ChiefComplaint = Clean(request.ChiefComplaint);
        visit.Examination = Clean(request.Examination);
        visit.Diagnosis = Clean(request.Diagnosis);
        visit.Notes = Clean(request.Notes);
        visit.TreatmentPlan = Clean(request.TreatmentPlan);
        visit.DocumentationStatus = DocumentationStatus.Completed;
        visit.UpdatedDate = DateTime.UtcNow;
        visit.UpdatedByUserId = userId;

        // Commit the state transition and its audit record in the same transaction.
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
        await transaction.CommitAsync(cancellationToken);

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
