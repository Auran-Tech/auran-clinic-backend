namespace Auran.Clinic.Application.Visits;

public sealed class StartVisitRequest
{
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
}

public sealed record VisitResponse(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    string Status,
    DateTime EntryAtUtc,
    Guid QueueEntryId,
    Guid WorkflowStatusId,
    string WorkflowStatusCode,
    string WorkflowStatusName);

public enum VisitStartOutcome
{
    Success,
    PatientNotFound,
    DoctorNotFound,
    ActiveVisitExists,
    WorkflowNotConfigured,
    Unauthenticated
}

public sealed record VisitStartResult(
    VisitStartOutcome Outcome,
    VisitResponse? Visit = null,
    string? Error = null);
