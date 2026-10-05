using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.FollowUps;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.FollowUps;

public sealed class FollowUpService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IFollowUpService
{
    public async Task<IReadOnlyCollection<FollowUpResponse>> ListAsync(
        string? search,
        string? dueCategory,
        CancellationToken cancellationToken = default)
    {
        var clinicDate = await GetClinicDateAsync(cancellationToken);
        var query = from followUp in dbContext.FollowUps.AsNoTracking()
                    join patient in dbContext.Patients.AsNoTracking()
                        on followUp.PatientId equals patient.Id
                    join doctor in dbContext.Users.AsNoTracking()
                        on followUp.DoctorId equals doctor.Id
                    orderby followUp.RecommendedDate, followUp.CreatedDate descending
                    select new
                    {
                        FollowUp = followUp,
                        PatientNumber = patient.PatientNumber,
                        PatientName = patient.FullName,
                        DoctorName = doctor.FullName
                    };

        var normalizedSearch = Clean(search);
        if (normalizedSearch is not null)
        {
            query = query.Where(item =>
                item.PatientName.Contains(normalizedSearch) ||
                item.PatientNumber.Contains(normalizedSearch) ||
                item.DoctorName.Contains(normalizedSearch) ||
                item.FollowUp.Recommendation.Contains(normalizedSearch));
        }

        var rows = await query.ToListAsync(cancellationToken);
        var mapped = rows
            .Select(item => Map(
                item.FollowUp,
                item.PatientNumber,
                item.PatientName,
                item.DoctorName,
                clinicDate))
            .ToList();

        var normalizedCategory = Clean(dueCategory);
        if (normalizedCategory is not null && !normalizedCategory.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            mapped = mapped
                .Where(item => item.DueCategory.Equals(normalizedCategory, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return mapped;
    }

    public async Task<FollowUpMutationResult> CreateAsync(
        CreateFollowUpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return new FollowUpMutationResult(FollowUpMutationOutcome.Unauthenticated);

        var patient = await dbContext.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.PatientId, cancellationToken);
        if (patient is null)
            return new FollowUpMutationResult(FollowUpMutationOutcome.NotFound, Error: "Patient not found.");

        var visit = await dbContext.Visits
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == request.VisitId && item.PatientId == request.PatientId,
                cancellationToken);
        if (visit is null)
            return new FollowUpMutationResult(FollowUpMutationOutcome.NotFound, Error: "Visit not found for this patient.");

        var doctor = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.DoctorId && item.IsActive, cancellationToken);
        if (doctor is null)
            return new FollowUpMutationResult(FollowUpMutationOutcome.NotFound, Error: "Doctor not found or inactive.");

        var clinicDate = await GetClinicDateAsync(cancellationToken);
        var recommendedDate = request.RecommendedDate;
        if (!recommendedDate.HasValue && request.RecommendedAfterDays.HasValue)
            recommendedDate = clinicDate.AddDays(request.RecommendedAfterDays.Value);

        var now = DateTime.UtcNow;
        var followUp = new FollowUp
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = request.PatientId,
            VisitId = request.VisitId,
            DoctorId = request.DoctorId,
            Recommendation = request.Recommendation.Trim(),
            RecommendedAfterDays = request.RecommendedAfterDays,
            RecommendedDate = recommendedDate,
            Status = FollowUpStatus.Open,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.FollowUps.Add(followUp);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "FollowUp.Created",
            nameof(FollowUp),
            followUp.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = request.PatientId,
                ["visitId"] = request.VisitId,
                ["doctorId"] = request.DoctorId,
                ["recommendedDate"] = recommendedDate?.ToString("yyyy-MM-dd")
            },
            cancellationToken);

        return new FollowUpMutationResult(
            FollowUpMutationOutcome.Success,
            Map(followUp, patient.PatientNumber, patient.FullName, doctor.FullName, clinicDate));
    }

    public async Task<FollowUpMutationResult> UpdateAsync(
        UpdateFollowUpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return new FollowUpMutationResult(FollowUpMutationOutcome.Unauthenticated);

        var followUp = await dbContext.FollowUps
            .SingleOrDefaultAsync(item => item.Id == request.FollowUpId, cancellationToken);
        if (followUp is null)
            return new FollowUpMutationResult(FollowUpMutationOutcome.NotFound, Error: "Follow-up not found.");
        if (followUp.Status != FollowUpStatus.Open)
            return new FollowUpMutationResult(FollowUpMutationOutcome.ValidationError, Error: "Only open follow-ups can be edited.");

        var clinicDate = await GetClinicDateAsync(cancellationToken);
        var recommendedDate = request.RecommendedDate;
        if (!recommendedDate.HasValue && request.RecommendedAfterDays.HasValue)
            recommendedDate = clinicDate.AddDays(request.RecommendedAfterDays.Value);

        followUp.Recommendation = request.Recommendation.Trim();
        followUp.RecommendedAfterDays = request.RecommendedAfterDays;
        followUp.RecommendedDate = recommendedDate;
        followUp.UpdatedDate = DateTime.UtcNow;
        followUp.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            "FollowUp.Updated",
            nameof(FollowUp),
            followUp.Id.ToString(),
            new Dictionary<string, object?> { ["recommendedDate"] = recommendedDate?.ToString("yyyy-MM-dd") },
            cancellationToken);

        return await MapResultAsync(followUp, clinicDate, cancellationToken);
    }

    public async Task<FollowUpMutationResult> SetStatusAsync(
        SetFollowUpStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return new FollowUpMutationResult(FollowUpMutationOutcome.Unauthenticated);

        var followUp = await dbContext.FollowUps
            .SingleOrDefaultAsync(item => item.Id == request.FollowUpId, cancellationToken);
        if (followUp is null)
            return new FollowUpMutationResult(FollowUpMutationOutcome.NotFound, Error: "Follow-up not found.");

        followUp.Status = request.Status;
        followUp.UpdatedDate = DateTime.UtcNow;
        followUp.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            request.Status == FollowUpStatus.Completed ? "FollowUp.Completed" : "FollowUp.Cancelled",
            nameof(FollowUp),
            followUp.Id.ToString(),
            null,
            cancellationToken);

        var clinicDate = await GetClinicDateAsync(cancellationToken);
        return await MapResultAsync(followUp, clinicDate, cancellationToken);
    }

    private async Task<FollowUpMutationResult> MapResultAsync(
        FollowUp followUp,
        DateOnly clinicDate,
        CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients.AsNoTracking()
            .SingleAsync(item => item.Id == followUp.PatientId, cancellationToken);
        var doctor = await dbContext.Users.AsNoTracking()
            .SingleAsync(item => item.Id == followUp.DoctorId, cancellationToken);

        return new FollowUpMutationResult(
            FollowUpMutationOutcome.Success,
            Map(followUp, patient.PatientNumber, patient.FullName, doctor.FullName, clinicDate));
    }

    private async Task<DateOnly> GetClinicDateAsync(CancellationToken cancellationToken)
    {
        var clinicId = currentUserContext.ClinicId;
        if (!clinicId.HasValue)
            return DateOnly.FromDateTime(DateTime.UtcNow);

        var timeZoneId = await dbContext.ClinicSettings.AsNoTracking()
            .Where(item => item.ClinicId == clinicId.Value)
            .Select(item => item.TimeZoneId)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(timeZoneId))
            return DateOnly.FromDateTime(DateTime.UtcNow);

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var clinicNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            return DateOnly.FromDateTime(clinicNow);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }
        catch (InvalidTimeZoneException)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }

    private static FollowUpResponse Map(
        FollowUp followUp,
        string patientNumber,
        string patientName,
        string doctorName,
        DateOnly clinicDate) =>
        new(
            followUp.Id,
            followUp.PatientId,
            followUp.VisitId,
            followUp.DoctorId,
            patientNumber,
            patientName,
            doctorName,
            followUp.Recommendation,
            followUp.RecommendedAfterDays,
            followUp.RecommendedDate,
            followUp.Status,
            GetDueCategory(followUp, clinicDate),
            followUp.CreatedDate,
            followUp.UpdatedDate);

    private static string GetDueCategory(FollowUp followUp, DateOnly clinicDate)
    {
        if (followUp.Status == FollowUpStatus.Completed)
            return "Completed";
        if (followUp.Status == FollowUpStatus.Cancelled)
            return "Cancelled";
        if (!followUp.RecommendedDate.HasValue)
            return "Open";
        if (followUp.RecommendedDate.Value < clinicDate)
            return "Overdue";
        if (followUp.RecommendedDate.Value == clinicDate)
            return "Today";
        return "Upcoming";
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
