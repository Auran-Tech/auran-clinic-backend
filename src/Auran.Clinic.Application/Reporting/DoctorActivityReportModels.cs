namespace Auran.Clinic.Application.Reporting;

public sealed class DoctorActivityReportQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public Guid? DoctorId { get; init; }
}

public sealed record DoctorActivityReportRowResponse(
    Guid DoctorId,
    string DoctorName,
    int CurrentQueue,
    int TotalVisits,
    int CompletedDocs,
    int PendingDocs);

public sealed record DoctorActivityReportResponse(
    DateOnly? FromDate,
    DateOnly? ToDate,
    int TotalCount,
    IReadOnlyList<DoctorActivityReportRowResponse> Rows);
