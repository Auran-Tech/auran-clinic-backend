namespace Auran.Clinic.Application.Reporting;

public sealed record DashboardSummaryResponse(
    DateOnly LocalDate,
    int TotalPatients,
    int VisitsToday,
    int ActiveQueue,
    int CompletedVisitsToday,
    int PendingDocumentation,
    int FollowUpsToday,
    int OverdueFollowUps);

public sealed class VisitReportQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public Guid? DoctorId { get; init; }
    public string? VisitStatus { get; init; }
    public string? DocumentationStatus { get; init; }
}

public sealed record VisitReportRowResponse(
    Guid VisitId,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    string VisitStatus,
    string DocumentationStatus,
    DateTime EntryAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ExitAtUtc,
    string? Diagnosis,
    string? TreatmentPlan);

public sealed record VisitReportResponse(
    DateOnly? FromDate,
    DateOnly? ToDate,
    int TotalCount,
    IReadOnlyList<VisitReportRowResponse> Rows);

public sealed record ReportExportResponse(
    byte[] Content,
    string ContentType,
    string FileName);
