using System.Globalization;
using System.Text;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Reporting;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Reporting;

public sealed class ReportingService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext) : IReportingService
{
    public async Task<DashboardSummaryResponse> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var (today, startUtc, endUtc) = await GetLocalDayWindowAsync(cancellationToken);

        var totalPatients = await dbContext.Patients.AsNoTracking().CountAsync(cancellationToken);
        var visitsToday = await dbContext.Visits.AsNoTracking()
            .CountAsync(visit => visit.EntryAtUtc >= startUtc && visit.EntryAtUtc < endUtc, cancellationToken);
        var activeQueue = await dbContext.QueueEntries.AsNoTracking()
            .CountAsync(entry => entry.ExitAtUtc == null, cancellationToken);
        var completedVisitsToday = await dbContext.Visits.AsNoTracking()
            .CountAsync(
                visit => visit.CompletedAtUtc >= startUtc && visit.CompletedAtUtc < endUtc,
                cancellationToken);
        var pendingDocumentation = await dbContext.Visits.AsNoTracking()
            .CountAsync(
                visit => visit.DocumentationStatus == DocumentationStatus.Draft ||
                         visit.DocumentationStatus == DocumentationStatus.Pending,
                cancellationToken);
        var followUpsToday = await dbContext.FollowUps.AsNoTracking()
            .CountAsync(
                item =>
                    item.Status == FollowUpStatus.Open &&
                    item.RecommendedDate == today,
                cancellationToken);
        var overdueFollowUps = await dbContext.FollowUps.AsNoTracking()
            .CountAsync(
                item =>
                    item.Status == FollowUpStatus.Open &&
                    item.RecommendedDate < today,
                cancellationToken);

        return new DashboardSummaryResponse(
            today,
            totalPatients,
            visitsToday,
            activeQueue,
            completedVisitsToday,
            pendingDocumentation,
            followUpsToday,
            overdueFollowUps);
    }

    public async Task<VisitReportResponse> GetVisitReportAsync(
        VisitReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var visits = dbContext.Visits.AsNoTracking();

        if (query.FromDate.HasValue)
        {
            var fromUtc = await ConvertLocalStartToUtcAsync(query.FromDate.Value, cancellationToken);
            visits = visits.Where(visit => visit.EntryAtUtc >= fromUtc);
        }

        if (query.ToDate.HasValue)
        {
            var toExclusiveUtc = await ConvertLocalStartToUtcAsync(
                query.ToDate.Value.AddDays(1),
                cancellationToken);
            visits = visits.Where(visit => visit.EntryAtUtc < toExclusiveUtc);
        }

        if (query.DoctorId.HasValue)
            visits = visits.Where(visit => visit.DoctorId == query.DoctorId.Value);

        if (!string.IsNullOrWhiteSpace(query.VisitStatus) &&
            Enum.TryParse<VisitStatus>(query.VisitStatus, ignoreCase: true, out var visitStatus))
        {
            visits = visits.Where(visit => visit.Status == visitStatus);
        }

        if (!string.IsNullOrWhiteSpace(query.DocumentationStatus) &&
            Enum.TryParse<DocumentationStatus>(
                query.DocumentationStatus,
                ignoreCase: true,
                out var documentationStatus))
        {
            visits = visits.Where(visit => visit.DocumentationStatus == documentationStatus);
        }

        var rows = await (
            from visit in visits
            join patient in dbContext.Patients.AsNoTracking() on visit.PatientId equals patient.Id
            join doctor in dbContext.Users.AsNoTracking() on visit.DoctorId equals doctor.Id
            orderby visit.EntryAtUtc descending
            select new VisitReportRowResponse(
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
                visit.Diagnosis,
                visit.TreatmentPlan))
            .ToListAsync(cancellationToken);

        return new VisitReportResponse(
            query.FromDate,
            query.ToDate,
            rows.Count,
            rows);
    }

    public async Task<ReportExportResponse> ExportVisitReportCsvAsync(
        VisitReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var report = await GetVisitReportAsync(query, cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("VisitId,PatientNumber,PatientName,DoctorName,VisitStatus,DocumentationStatus,EntryAtUtc,CompletedAtUtc,ExitAtUtc,Diagnosis,TreatmentPlan");

        foreach (var row in report.Rows)
        {
            csv.Append(Escape(row.VisitId.ToString())).Append(',')
                .Append(Escape(row.PatientNumber)).Append(',')
                .Append(Escape(row.PatientName)).Append(',')
                .Append(Escape(row.DoctorName)).Append(',')
                .Append(Escape(row.VisitStatus)).Append(',')
                .Append(Escape(row.DocumentationStatus)).Append(',')
                .Append(Escape(row.EntryAtUtc.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                .Append(Escape(row.CompletedAtUtc?.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                .Append(Escape(row.ExitAtUtc?.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                .Append(Escape(row.Diagnosis)).Append(',')
                .Append(Escape(row.TreatmentPlan))
                .AppendLine();
        }

        var from = report.FromDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "all";
        var to = report.ToDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "all";
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString());

        return new ReportExportResponse(
            bytes,
            "text/csv; charset=utf-8",
            $"auran-visits-{from}-{to}.csv");
    }

    private async Task<(DateOnly Today, DateTime StartUtc, DateTime EndUtc)> GetLocalDayWindowAsync(
        CancellationToken cancellationToken)
    {
        var timeZone = await GetClinicTimeZoneAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        var today = DateOnly.FromDateTime(localNow);
        var startUtc = ConvertLocalStartToUtc(today, timeZone);
        var endUtc = ConvertLocalStartToUtc(today.AddDays(1), timeZone);
        return (today, startUtc, endUtc);
    }

    private async Task<DateTime> ConvertLocalStartToUtcAsync(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var timeZone = await GetClinicTimeZoneAsync(cancellationToken);
        return ConvertLocalStartToUtc(date, timeZone);
    }

    private async Task<TimeZoneInfo> GetClinicTimeZoneAsync(CancellationToken cancellationToken)
    {
        if (!currentUserContext.IsAuthenticated || currentUserContext.ClinicId is not Guid clinicId)
            return TimeZoneInfo.Utc;

        var timeZoneId = await dbContext.Clinics.AsNoTracking()
            .Where(clinic => clinic.Id == clinicId)
            .Select(clinic => clinic.TimeZoneId)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
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

    private static DateTime ConvertLocalStartToUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (!text.Contains(',') && !text.Contains('"') && !text.Contains('\n') && !text.Contains('\r'))
            return text;

        var escaped = text.Replace(""", """");
        return """ + escaped + """;
    }
}
