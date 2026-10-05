namespace Auran.Clinic.Application.Reports;

public sealed record DoctorActivityReportResponse(
    string DoctorName,
    int VisitCount,
    int CompletedVisitCount,
    int PendingDocumentationCount);

public sealed record OperationalReportResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    int PatientCount,
    int NewPatientCount,
    int VisitCount,
    int OpenVisitCount,
    int CompletedVisitCount,
    int ActiveQueueCount,
    int PendingDocumentationCount,
    int FollowUpsToday,
    int FollowUpsOverdue,
    IReadOnlyCollection<DoctorActivityReportResponse> DoctorActivity);
