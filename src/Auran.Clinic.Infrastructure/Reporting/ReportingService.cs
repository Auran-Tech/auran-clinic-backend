using System.Globalization;
using System.Text;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Reporting;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Auran.Clinic.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Reporting;

public sealed class ReportingService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext) : IReportingService
{
    public async Task<DoctorActivityReportResponse> GetDoctorActivityReportAsync(
        DoctorActivityReportQuery query, CancellationToken cancellationToken = default)
    {
        var doctorIds = dbContext.UserRoles.AsNoTracking()
            .Where(userRole => dbContext.Roles.AsNoTracking()
                .Any(role => role.Id == userRole.RoleId && role.Code == "DOCTOR"))
            .Select(userRole => userRole.UserId)
            .Distinct();

        var doctors = dbContext.Users.AsNoTracking()
            .Where(user => doctorIds.Contains(user.Id));
        if (query.DoctorId.HasValue)
            doctors = doctors.Where(user => user.Id == query.DoctorId.Value);

        var visits = dbContext.Visits.AsNoTracking();
        if (query.FromDate.HasValue)
        {
            var fromUtc = await ConvertLocalStartToUtcAsync(query.FromDate.Value, cancellationToken);
            visits = visits.Where(visit => visit.EntryAtUtc >= fromUtc);
        }
        if (query.ToDate.HasValue)
        {
            var toExclusiveUtc = await ConvertLocalStartToUtcAsync(query.ToDate.Value.AddDays(1), cancellationToken);
            visits = visits.Where(visit => visit.EntryAtUtc < toExclusiveUtc);
        }

        // Counts are evaluated in SQL and retain the clinic query filters.
        // Current queue is intentionally a live count, independent of the visit date range.
        var rows = await doctors
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Id)
            .Select(user => new DoctorActivityReportRowResponse(
                user.Id,
                user.FullName,
                dbContext.QueueEntries.AsNoTracking().Count(entry => entry.DoctorId == user.Id && entry.ExitAtUtc == null),
                visits.Count(visit => visit.DoctorId == user.Id),
                visits.Count(visit => visit.DoctorId == user.Id &&
                    visit.DocumentationStatus == DocumentationStatus.Completed),
                visits.Count(visit => visit.DoctorId == user.Id &&
                    visit.DocumentationStatus != DocumentationStatus.Completed)))
            .Take(VisitReportSizePolicy.MaximumRows + 1)
            .ToListAsync(cancellationToken);
        VisitReportSizePolicy.EnsureWithinLimit(rows.Count);

        return new DoctorActivityReportResponse(query.FromDate, query.ToDate, rows.Count, rows);
    }

    public async Task<ReportExportResponse> ExportDoctorActivityCsvAsync(
        DoctorActivityReportQuery query, CancellationToken cancellationToken = default)
    {
        var report = await GetDoctorActivityReportAsync(query, cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("DoctorName,CurrentQueue,TotalVisits,CompletedDocs,PendingDocs");
        foreach (var row in report.Rows)
        {
            csv.Append(ReportCsvCell.Encode(row.DoctorName)).Append(',')
                .Append(row.CurrentQueue.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.TotalVisits.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.CompletedDocs.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.PendingDocs.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }
        var from = report.FromDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "all";
        var to = report.ToDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "all";
        return new ReportExportResponse(
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString()),
            "text/csv; charset=utf-8",
            $"auran-doctors-{from}-{to}.csv");
    }

    public async Task<QueueReportResponse> GetQueueReportAsync(
        QueueReportQuery query, CancellationToken cancellationToken = default)
    {
        // Every joined DbSet retains its clinic-scoped EF query filter.
        var queue = dbContext.QueueEntries.AsNoTracking();
        if (query.FromDate.HasValue)
        {
            var start = await ConvertLocalStartToUtcAsync(query.FromDate.Value, cancellationToken);
            queue = queue.Where(entry => entry.EntryAtUtc >= start);
        }
        if (query.ToDate.HasValue)
        {
            var end = await ConvertLocalStartToUtcAsync(query.ToDate.Value.AddDays(1), cancellationToken);
            queue = queue.Where(entry => entry.EntryAtUtc < end);
        }
        if (query.DoctorId.HasValue)
            queue = queue.Where(entry => entry.DoctorId == query.DoctorId);
        if (query.WorkflowStatusId.HasValue)
            queue = queue.Where(entry => entry.WorkflowStatusId == query.WorkflowStatusId);

        var projected = await (
            from entry in queue
            join patient in dbContext.Patients.AsNoTracking() on entry.PatientId equals patient.Id
            join status in dbContext.WorkflowStatuses.AsNoTracking() on entry.WorkflowStatusId equals status.Id
            join doctor in dbContext.Users.AsNoTracking() on entry.DoctorId equals (Guid?)doctor.Id into doctors
            from doctor in doctors.DefaultIfEmpty()
            orderby entry.EntryAtUtc descending, entry.Id
            select new
            {
                entry.Id, entry.PatientId, patient.PatientNumber, PatientName = patient.FullName,
                entry.DoctorId, DoctorName = doctor == null ? null : doctor.FullName,
                entry.WorkflowStatusId, StatusCode = status.Code, StatusName = status.Name,
                entry.EntryAtUtc, entry.ExitAtUtc
            })
            .Take(VisitReportSizePolicy.MaximumRows + 1)
            .ToListAsync(cancellationToken);
        VisitReportSizePolicy.EnsureWithinLimit(projected.Count);

        var rows = projected.Select(item => new QueueReportRowResponse(
            item.Id, item.PatientId, item.PatientNumber, item.PatientName,
            item.DoctorId, item.DoctorName, item.WorkflowStatusId,
            item.StatusCode, item.StatusName, item.EntryAtUtc, item.ExitAtUtc,
            item.ExitAtUtc.HasValue
                ? Math.Max(0, (item.ExitAtUtc.Value - item.EntryAtUtc).TotalMinutes)
                : null)).ToList();

        return new QueueReportResponse(query.FromDate, query.ToDate, rows.Count, rows);
    }

    public async Task<ReportExportResponse> ExportQueueReportCsvAsync(
        QueueReportQuery query, CancellationToken cancellationToken = default)
    {
        var report = await GetQueueReportAsync(query, cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("PatientNumber,PatientName,DoctorName,WorkflowStatus,EntryAtUtc,ExitAtUtc,DurationMinutes");
        foreach (var row in report.Rows)
        {
            csv.Append(ReportCsvCell.Encode(row.PatientNumber)).Append(',')
                .Append(ReportCsvCell.Encode(row.PatientName)).Append(',')
                .Append(ReportCsvCell.Encode(row.DoctorName)).Append(',')
                .Append(ReportCsvCell.Encode(row.WorkflowStatusName)).Append(',')
                .Append(ReportCsvCell.Encode(row.EntryAtUtc.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                .Append(ReportCsvCell.Encode(row.ExitAtUtc?.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                .Append(row.DurationMinutes?.ToString("F2", CultureInfo.InvariantCulture))
                .AppendLine();
        }
        var from = report.FromDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "all";
        var to = report.ToDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "all";
        return new ReportExportResponse(
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString()),
            "text/csv; charset=utf-8",
            $"auran-queue-{from}-{to}.csv");
    }

    public async Task<PatientReportResponse> GetPatientReportAsync(
        PatientReportQuery query,
        CancellationToken cancellationToken = default)
    {
        // The DbContext applies the clinic/tenant query filter to Patients.
        var patients = dbContext.Patients.AsNoTracking();
        if (query.FromDate.HasValue)
        {
            var fromUtc = await ConvertLocalStartToUtcAsync(query.FromDate.Value, cancellationToken);
            patients = patients.Where(patient => patient.CreatedDate >= fromUtc);
        }

        if (query.ToDate.HasValue)
        {
            var exclusiveUtc = await ConvertLocalStartToUtcAsync(query.ToDate.Value.AddDays(1), cancellationToken);
            patients = patients.Where(patient => patient.CreatedDate < exclusiveUtc);
        }

        if (!string.IsNullOrWhiteSpace(query.Gender))
        {
            var gender = query.Gender.Trim();
            patients = patients.Where(patient => patient.Gender == gender);
        }

        var rows = await patients
            .OrderBy(patient => patient.CreatedDate)
            .ThenBy(patient => patient.Id)
            .Select(patient => new PatientReportRowResponse(
                patient.Id, patient.PatientNumber, patient.FullName, patient.Phone,
                patient.Gender, patient.DateOfBirth, patient.CreatedDate))
            .Take(VisitReportSizePolicy.MaximumRows + 1)
            .ToListAsync(cancellationToken);

        VisitReportSizePolicy.EnsureWithinLimit(rows.Count);
        return new PatientReportResponse(query.FromDate, query.ToDate, rows.Count, rows);
    }

    public async Task<ReportExportResponse> ExportPatientReportCsvAsync(
        PatientReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var report = await GetPatientReportAsync(query, cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("PatientNumber,PatientName,Phone,Gender,DateOfBirth,CreatedAtUtc");
        foreach (var row in report.Rows)
        {
            csv.Append(ReportCsvCell.Encode(row.PatientNumber)).Append(',')
                .Append(ReportCsvCell.Encode(row.PatientName)).Append(',')
                .Append(ReportCsvCell.Encode(row.Phone)).Append(',')
                .Append(ReportCsvCell.Encode(row.Gender)).Append(',')
                .Append(ReportCsvCell.Encode(row.DateOfBirth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).Append(',')
                .Append(ReportCsvCell.Encode(row.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)))
                .AppendLine();
        }

        var from = report.FromDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "all";
        var to = report.ToDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "all";
        return new ReportExportResponse(
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString()),
            "text/csv; charset=utf-8",
            $"auran-patients-{from}-{to}.csv");
    }

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
        var completedVisitsStartedToday = await dbContext.Visits.AsNoTracking()
            .CountAsync(
                visit => visit.EntryAtUtc >= startUtc &&
                         visit.EntryAtUtc < endUtc &&
                         visit.CompletedAtUtc != null,
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

        var timeZone = await GetClinicTimeZoneAsync(cancellationToken);
        var weeklyVisits = new List<DashboardDailyVisitsResponse>(7);
        for (var offset = -6; offset < 0; offset++)
        {
            var day = today.AddDays(offset);
            var dayStartUtc = ConvertLocalStartToUtc(day, timeZone);
            var nextDayStartUtc = ConvertLocalStartToUtc(day.AddDays(1), timeZone);
            var count = await dbContext.Visits.AsNoTracking()
                .CountAsync(
                    visit => visit.EntryAtUtc >= dayStartUtc &&
                             visit.EntryAtUtc < nextDayStartUtc,
                    cancellationToken);
            weeklyVisits.Add(new DashboardDailyVisitsResponse(day, count));
        }
        weeklyVisits.Add(new DashboardDailyVisitsResponse(today, visitsToday));

        return new DashboardSummaryResponse(
            today,
            totalPatients,
            visitsToday,
            activeQueue,
            completedVisitsToday,
            completedVisitsStartedToday,
            pendingDocumentation,
            followUpsToday,
            overdueFollowUps,
            weeklyVisits);
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
            .Take(VisitReportSizePolicy.MaximumRows + 1)
            .ToListAsync(cancellationToken);

        VisitReportSizePolicy.EnsureWithinLimit(rows.Count);

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
        => await ClinicLocalDate.GetTimeZoneAsync(
            dbContext, currentUserContext, cancellationToken);

    private static DateTime ConvertLocalStartToUtc(DateOnly date, TimeZoneInfo timeZone)
        => ClinicLocalDate.StartUtc(date, timeZone);

    private static string Escape(string? value)
    {
        return ReportCsvCell.Encode(value);
    }

}
