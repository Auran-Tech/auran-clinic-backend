namespace Auran.Clinic.Application.Reporting;

public sealed class PatientReportQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public string? Gender { get; init; }
}

public sealed record PatientReportRowResponse(
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    string Phone,
    string? Gender,
    DateOnly? DateOfBirth,
    DateTime CreatedAtUtc);

public sealed record PatientReportResponse(
    DateOnly? FromDate,
    DateOnly? ToDate,
    int TotalCount,
    IReadOnlyList<PatientReportRowResponse> Rows);
