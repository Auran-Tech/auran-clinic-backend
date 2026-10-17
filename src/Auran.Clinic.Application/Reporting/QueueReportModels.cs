namespace Auran.Clinic.Application.Reporting;

public sealed class QueueReportQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public Guid? DoctorId { get; init; }
    public Guid? WorkflowStatusId { get; init; }
}

public sealed record QueueReportRowResponse(
    Guid QueueEntryId,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid? DoctorId,
    string? DoctorName,
    Guid WorkflowStatusId,
    string WorkflowStatusCode,
    string WorkflowStatusName,
    DateTime EntryAtUtc,
    DateTime? ExitAtUtc,
    double? DurationMinutes);

public sealed record QueueReportResponse(
    DateOnly? FromDate,
    DateOnly? ToDate,
    int TotalCount,
    IReadOnlyList<QueueReportRowResponse> Rows);
