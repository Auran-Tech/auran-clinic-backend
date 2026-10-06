namespace Auran.Clinic.Application.Queue;

public sealed record QueueEntryResponse(
    Guid QueueEntryId,
    Guid VisitId,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid? DoctorId,
    string? DoctorName,
    Guid WorkflowStatusId,
    string WorkflowStatusCode,
    string WorkflowStatusName,
    string WorkflowStatusColor,
    DateTime EntryAtUtc,
    DateTime? ExitAtUtc);

public sealed record QueueTransitionOptionResponse(
    Guid WorkflowStatusId,
    string Code,
    string Name,
    string Color,
    bool IsSystemFinal);

public sealed class MoveQueueEntryRequest
{
    public Guid QueueEntryId { get; init; }
    public Guid ToWorkflowStatusId { get; init; }
    public string? Notes { get; init; }
}

public enum QueueMoveOutcome
{
    Success,
    NotFound,
    InvalidTransition,
    AlreadyExited,
    Unauthenticated
}

public sealed record QueueMoveResult(
    QueueMoveOutcome Outcome,
    QueueEntryResponse? Entry = null,
    string? Error = null);
