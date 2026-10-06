using FluentValidation;

namespace Auran.Clinic.Application.Queue;

public sealed record QueueWorkflowStatusResponse(
    Guid Id,
    string Code,
    string Name,
    string Color,
    int SortOrder,
    bool IsFinal);

public sealed record QueueEntryResponse(
    Guid Id,
    Guid PatientId,
    Guid VisitId,
    Guid? DoctorId,
    string PatientNumber,
    string PatientName,
    string? DoctorName,
    Guid WorkflowStatusId,
    string WorkflowStatusCode,
    string WorkflowStatusName,
    string WorkflowStatusColor,
    DateTime EntryAtUtc,
    DateTime? ExitAtUtc,
    string RowVersion);

public sealed record QueueStaffResponse(
    Guid Id,
    string FullName);

public sealed record QueueTransitionResponse(
    Guid FromStatusId,
    Guid ToStatusId);

public sealed record QueueBoardResponse(
    IReadOnlyCollection<QueueWorkflowStatusResponse> Statuses,
    IReadOnlyCollection<QueueEntryResponse> Entries,
    IReadOnlyCollection<QueueStaffResponse> Staff,
    IReadOnlyCollection<QueueTransitionResponse> Transitions);

public sealed class QueueCheckInRequest
{
    public Guid PatientId { get; init; }
    public Guid DoctorId { get; init; }
}

public sealed class QueueMoveRequest
{
    public Guid QueueEntryId { get; init; }
    public Guid ToStatusId { get; init; }
    public required string RowVersion { get; init; }
    public string? Notes { get; init; }
}

public enum QueueMutationOutcome
{
    Success,
    NotFound,
    Conflict,
    ConfigurationRequired,
    ValidationError,
    Unauthenticated
}

public sealed record QueueMutationResult(
    QueueMutationOutcome Outcome,
    QueueEntryResponse? Entry = null,
    string? Error = null);

public sealed class QueueCheckInRequestValidator : AbstractValidator<QueueCheckInRequest>
{
    public QueueCheckInRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
    }
}

public sealed class QueueMoveRequestValidator : AbstractValidator<QueueMoveRequest>
{
    public QueueMoveRequestValidator()
    {
        RuleFor(x => x.QueueEntryId).NotEmpty();
        RuleFor(x => x.ToStatusId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
