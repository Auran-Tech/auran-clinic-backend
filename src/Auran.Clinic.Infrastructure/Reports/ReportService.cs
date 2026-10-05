using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Reports;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Reports;

public sealed class ReportService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext) : IReportService
{
    public async Task<OperationalReportResponse> GetOperationalAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var clinicTimeZone = await GetClinicTimeZoneAsync(cancellationToken);
        var clinicToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, clinicTimeZone));
        var from = fromDate ?? clinicToday.AddDays(-29);
        var to = toDate ?? clinicToday;
        if (to < from)
            (from, to) = (to, from);

        var fromUtc = ToUtc(from, TimeOnly.MinValue, clinicTimeZone);
        var toUtcExclusive = ToUtc(to.AddDays(1), TimeOnly.MinValue, clinicTimeZone);

        var patientCount = await dbContext.Patients.AsNoTracking().CountAsync(cancellationToken);
        var newPatientCount = await dbContext.Patients.AsNoTracking()
            .CountAsync(item => item.CreatedDate >= fromUtc && item.CreatedDate < toUtcExclusive, cancellationToken);

        var visitsQuery = dbContext.Visits.AsNoTracking()
            .Where(item => item.EntryAtUtc >= fromUtc && item.EntryAtUtc < toUtcExclusive);

        var visitCount = await visitsQuery.CountAsync(cancellationToken);
        var openVisitCount = await visitsQuery.CountAsync(item => item.Status == VisitStatus.Open, cancellationToken);
        var completedVisitCount = await visitsQuery.CountAsync(item => item.Status == VisitStatus.Completed, cancellationToken);

        var activeQueueCount = await dbContext.QueueEntries.AsNoTracking()
            .CountAsync(item => item.ExitAtUtc == null, cancellationToken);

        var pendingDocumentationCount = await dbContext.Visits.AsNoTracking()
            .CountAsync(item => item.DocumentationStatus == DocumentationStatus.Pending, cancellationToken);

        var followUpsToday = await dbContext.FollowUps.AsNoTracking()
            .CountAsync(item =>
                item.Status == FollowUpStatus.Open &&
                item.RecommendedDate == clinicToday,
                cancellationToken);

        var followUpsOverdue = await dbContext.FollowUps.AsNoTracking()
            .CountAsync(item =>
                item.Status == FollowUpStatus.Open &&
                item.RecommendedDate.HasValue &&
                item.RecommendedDate.Value < clinicToday,
                cancellationToken);

        var doctorActivityRows = await (
                from visit in visitsQuery
                join doctor in dbContext.Users.AsNoTracking()
                    on visit.DoctorId equals doctor.Id
                group visit by new { doctor.Id, doctor.FullName } into groupRow
                orderby groupRow.Count() descending
                select new
                {
                    groupRow.Key.Id,
                    groupRow.Key.FullName,
                    VisitCount = groupRow.Count(),
                    CompletedVisitCount = groupRow.Count(item => item.Status == VisitStatus.Completed),
                    PendingDocumentationCount = groupRow.Count(item => item.DocumentationStatus == DocumentationStatus.Pending)
                })
            .ToListAsync(cancellationToken);

        return new OperationalReportResponse(
            from,
            to,
            patientCount,
            newPatientCount,
            visitCount,
            openVisitCount,
            completedVisitCount,
            activeQueueCount,
            pendingDocumentationCount,
            followUpsToday,
            followUpsOverdue,
            doctorActivityRows
                .Select(item => new DoctorActivityReportResponse(
                    item.FullName,
                    item.VisitCount,
                    item.CompletedVisitCount,
                    item.PendingDocumentationCount))
                .ToArray());
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

    private static DateTime ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo timeZone)
    {
        var unspecified = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone);
    }
}
