namespace Auran.Clinic.Application.Dashboard;

public sealed record DashboardQueueItemResponse(
    string PatientNumber,
    string PatientName,
    string StatusName,
    string StatusColor,
    string? DoctorName,
    DateTime EntryAtUtc);

public sealed record DashboardFollowUpResponse(
    string PatientNumber,
    string PatientName,
    string Recommendation,
    DateOnly? RecommendedDate,
    string DueCategory);

public sealed record DashboardResponse(
    int TotalPatients,
    int NewPatientsToday,
    int ActiveQueueCount,
    int OpenVisitsCount,
    int PendingDocumentationCount,
    int FollowUpsToday,
    int FollowUpsOverdue,
    IReadOnlyCollection<DashboardQueueItemResponse> Queue,
    IReadOnlyCollection<DashboardFollowUpResponse> FollowUps);
