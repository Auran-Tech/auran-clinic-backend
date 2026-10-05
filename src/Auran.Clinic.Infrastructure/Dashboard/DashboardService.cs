using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Dashboard;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Dashboard;

public sealed class DashboardService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext) : IDashboardService
{
    public async Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var timeZone = await GetClinicTimeZoneAsync(cancellationToken);
        var clinicToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        var todayStartUtc = ToUtc(clinicToday, timeZone);
        var tomorrowStartUtc = ToUtc(clinicToday.AddDays(1), timeZone);

        var totalPatients = await dbContext.Patients.AsNoTracking().CountAsync(cancellationToken);
        var newPatientsToday = await dbContext.Patients.AsNoTracking()
            .CountAsync(item => item.CreatedDate >= todayStartUtc && item.CreatedDate < tomorrowStartUtc, cancellationToken);
        var activeQueueCount = await dbContext.QueueEntries.AsNoTracking()
            .CountAsync(item => item.ExitAtUtc == null, cancellationToken);
        var openVisitsCount = await dbContext.Visits.AsNoTracking()
            .CountAsync(item => item.Status == VisitStatus.Open, cancellationToken);
        var pendingDocumentationCount = await dbContext.Visits.AsNoTracking()
            .CountAsync(item => item.DocumentationStatus == DocumentationStatus.Pending, cancellationToken);
        var followUpsToday = await dbContext.FollowUps.AsNoTracking()
            .CountAsync(item => item.Status == FollowUpStatus.Open && item.RecommendedDate == clinicToday, cancellationToken);
        var followUpsOverdue = await dbContext.FollowUps.AsNoTracking()
            .CountAsync(item => item.Status == FollowUpStatus.Open && item.RecommendedDate.HasValue && item.RecommendedDate.Value < clinicToday, cancellationToken);

        var queue = await (
                from entry in dbContext.QueueEntries.AsNoTracking()
                join patient in dbContext.Patients.AsNoTracking() on entry.PatientId equals patient.Id
                join status in dbContext.WorkflowStatuses.AsNoTracking() on entry.WorkflowStatusId equals status.Id
                join doctor in dbContext.Users.AsNoTracking() on entry.DoctorId equals doctor.Id into doctorJoin
                from doctor in doctorJoin.DefaultIfEmpty()
                where entry.ExitAtUtc == null
                orderby status.SortOrder, entry.EntryAtUtc
                select new DashboardQueueItemResponse(
                    patient.PatientNumber,
                    patient.FullName,
                    status.Name,
                    status.Color,
                    doctor == null ? null : doctor.FullName,
                    entry.EntryAtUtc))
            .Take(8)
            .ToListAsync(cancellationToken);

        var followUpRows = await (
                from followUp in dbContext.FollowUps.AsNoTracking()
                join patient in dbContext.Patients.AsNoTracking() on followUp.PatientId equals patient.Id
                where followUp.Status == FollowUpStatus.Open
                      && followUp.RecommendedDate.HasValue
                      && followUp.RecommendedDate.Value <= clinicToday
                orderby followUp.RecommendedDate
                select new
                {
                    followUp.Recommendation,
                    followUp.RecommendedDate,
                    PatientNumber = patient.PatientNumber,
                    PatientName = patient.FullName
                })
            .Take(8)
            .ToListAsync(cancellationToken);

        var followUps = followUpRows
            .Select(item => new DashboardFollowUpResponse(
                item.PatientNumber,
                item.PatientName,
                item.Recommendation,
                item.RecommendedDate,
                item.RecommendedDate == clinicToday ? "Today" : "Overdue"))
            .ToArray();

        return new DashboardResponse(
            totalPatients,
            newPatientsToday,
            activeQueueCount,
            openVisitsCount,
            pendingDocumentationCount,
            followUpsToday,
            followUpsOverdue,
            queue,
            followUps);
    }

    private async Task<TimeZoneInfo> GetClinicTimeZoneAsync(CancellationToken cancellationToken)
    {
        if (!currentUserContext.ClinicId.HasValue)
            return TimeZoneInfo.Utc;

        var id = await dbContext.Clinics.AsNoTracking()
            .Where(item => item.Id == currentUserContext.ClinicId.Value)
            .Select(item => item.TimeZoneId)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(id))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static DateTime ToUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }
}
