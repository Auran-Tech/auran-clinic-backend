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
    public async Task<IReadOnlyList<FollowUpResponse>> ListAsync(
        FollowUpQuery query,
        CancellationToken cancellationToken = default)
    {
        var followUps = dbContext.FollowUps.AsNoTracking();

        if (query.Bucket == FollowUpBucket.Completed)
        {
            followUps = followUps.Where(item => item.Status == FollowUpStatus.Completed);
        }
        else if (query.Bucket != FollowUpBucket.All)
        {
            followUps = followUps.Where(item => item.Status == FollowUpStatus.Open);
        }

        var raw = await (
            from followUp in followUps
            join patient in dbContext.Patients.AsNoTracking() on followUp.PatientId equals patient.Id
            join doctor in dbContext.Users.AsNoTracking() on followUp.DoctorId equals doctor.Id
            orderby followUp.RecommendedDate, patient.FullName
            select new
            {
                FollowUp = followUp,
                Patient = patient,
                Doctor = doctor
            })
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return raw
            .Select(item =>
            {
                var bucket = GetBucket(item.FollowUp, today);
                return new
                {
                    Bucket = bucket,
                    Response = Map(item.FollowUp, item.Patient, item.Doctor, bucket)
                };
            })
            .Where(item => query.Bucket == FollowUpBucket.All || item.Bucket == query.Bucket)
            .Select(item => item.Response)
            .ToList();
    }

    public async Task<FollowUpResult> CreateAsync(
        CreateFollowUpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId))
            return new FollowUpResult(FollowUpOutcome.Unauthenticated);

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);
        if (visit is null)
            return new FollowUpResult(FollowUpOutcome.VisitNotFound);

        if (visit.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new FollowUpResult(FollowUpOutcome.Forbidden);

        var recommendedDate = request.RecommendedDate;
        if (!recommendedDate.HasValue && request.RecommendedAfterDays.HasValue)
        {
            var basis = visit.ExitAtUtc ?? visit.CompletedAtUtc ?? DateTime.UtcNow;
            recommendedDate = DateOnly.FromDateTime(basis).AddDays(request.RecommendedAfterDays.Value);
        }

        var now = DateTime.UtcNow;
        var followUp = new FollowUp
        {
            Id = Guid.NewGuid(),
            ClinicId = visit.ClinicId,
            PatientId = visit.PatientId,
            VisitId = visit.Id,
            DoctorId = visit.DoctorId,
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
                ["visitId"] = followUp.VisitId,
                ["patientId"] = followUp.PatientId,
                ["recommendedDate"] = followUp.RecommendedDate
            },
            cancellationToken);

        return new FollowUpResult(
            FollowUpOutcome.Success,
            await MapAsync(followUp, cancellationToken));
    }

    public Task<FollowUpResult> CompleteAsync(
        ChangeFollowUpStatusRequest request,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(request, FollowUpStatus.Completed, "FollowUp.Completed", cancellationToken);

    public Task<FollowUpResult> CancelAsync(
        ChangeFollowUpStatusRequest request,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(request, FollowUpStatus.Cancelled, "FollowUp.Cancelled", cancellationToken);

    private async Task<FollowUpResult> ChangeStatusAsync(
        ChangeFollowUpStatusRequest request,
        FollowUpStatus targetStatus,
        string auditAction,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentActor(out var userId))
            return new FollowUpResult(FollowUpOutcome.Unauthenticated);

        var followUp = await dbContext.FollowUps
            .SingleOrDefaultAsync(item => item.Id == request.FollowUpId, cancellationToken);
        if (followUp is null)
            return new FollowUpResult(FollowUpOutcome.FollowUpNotFound);

        if (followUp.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new FollowUpResult(FollowUpOutcome.Forbidden);

        if (followUp.Status != FollowUpStatus.Open)
            return new FollowUpResult(FollowUpOutcome.InvalidState);

        followUp.Status = targetStatus;
        followUp.UpdatedDate = DateTime.UtcNow;
        followUp.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            auditAction,
            nameof(FollowUp),
            followUp.Id.ToString(),
            cancellationToken: cancellationToken);

        return new FollowUpResult(
            FollowUpOutcome.Success,
            await MapAsync(followUp, cancellationToken));
    }

    private async Task<FollowUpResponse> MapAsync(
        FollowUp followUp,
        CancellationToken cancellationToken)
    {
        var result = await (
            from patient in dbContext.Patients.AsNoTracking()
            join doctor in dbContext.Users.AsNoTracking() on followUp.DoctorId equals doctor.Id
            where patient.Id == followUp.PatientId
            select new
            {
                Patient = patient,
                Doctor = doctor
            })
            .SingleAsync(cancellationToken);

        var bucket = GetBucket(followUp, DateOnly.FromDateTime(DateTime.UtcNow));
        return Map(followUp, result.Patient, result.Doctor, bucket);
    }

    private static FollowUpResponse Map(
        FollowUp followUp,
        Patient patient,
        User doctor,
        FollowUpBucket bucket) =>
        new(
            followUp.Id,
            patient.Id,
            patient.PatientNumber,
            patient.FullName,
            followUp.VisitId,
            doctor.Id,
            doctor.FullName,
            followUp.Recommendation,
            followUp.RecommendedAfterDays,
            followUp.RecommendedDate,
            followUp.Status.ToString(),
            bucket.ToString());

    private static FollowUpBucket GetBucket(FollowUp followUp, DateOnly today)
    {
        if (followUp.Status == FollowUpStatus.Completed)
            return FollowUpBucket.Completed;

        if (followUp.Status != FollowUpStatus.Open || !followUp.RecommendedDate.HasValue)
            return FollowUpBucket.All;

        if (followUp.RecommendedDate.Value < today)
            return FollowUpBucket.Overdue;

        if (followUp.RecommendedDate.Value == today)
            return FollowUpBucket.Today;

        return FollowUpBucket.Upcoming;
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
