namespace Auran.Clinic.Application.FollowUps;

public enum FollowUpBucket
{
    All,
    Today,
    Upcoming,
    Overdue,
    Completed
}

public sealed class FollowUpQuery
{
    public FollowUpBucket Bucket { get; init; } = FollowUpBucket.All;
}

public sealed class CreateFollowUpRequest
{
    public Guid VisitId { get; init; }
    public string Recommendation { get; init; } = string.Empty;
    public int? RecommendedAfterDays { get; init; }
    public DateOnly? RecommendedDate { get; init; }
}

public sealed class ChangeFollowUpStatusRequest
{
    public Guid FollowUpId { get; init; }
}

public sealed record FollowUpResponse(
    Guid Id,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid VisitId,
    Guid DoctorId,
    string DoctorName,
    string Recommendation,
    int? RecommendedAfterDays,
    DateOnly? RecommendedDate,
    string Status,
    string Bucket);

public enum FollowUpOutcome
{
    Success,
    VisitNotFound,
    FollowUpNotFound,
    Forbidden,
    InvalidState,
    Unauthenticated
}

public sealed record FollowUpResult(
    FollowUpOutcome Outcome,
    FollowUpResponse? FollowUp = null);
