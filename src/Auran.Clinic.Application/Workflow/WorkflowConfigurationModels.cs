namespace Auran.Clinic.Application.Workflow;

public sealed record WorkflowStatusResponse(
    Guid Id,
    string Code,
    string Name,
    string Color,
    int SortOrder,
    bool IsSystemFinal,
    bool IsInUse);

public sealed record WorkflowTransitionResponse(
    Guid Id,
    Guid FromStatusId,
    Guid ToStatusId);

public sealed record WorkflowConfigurationResponse(
    IReadOnlyList<WorkflowStatusResponse> Statuses,
    IReadOnlyList<WorkflowTransitionResponse> Transitions);

public class CreateWorkflowStatusRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = "#64748b";
    public int SortOrder { get; init; }
    public bool IsSystemFinal { get; init; }
}

public sealed class UpdateWorkflowStatusRequest : CreateWorkflowStatusRequest
{
    public Guid StatusId { get; init; }
}

public sealed class DeleteWorkflowStatusRequest
{
    public Guid StatusId { get; init; }
}

public sealed class WorkflowTransitionInput
{
    public Guid FromStatusId { get; init; }
    public Guid ToStatusId { get; init; }
}

public sealed class ReplaceWorkflowTransitionsRequest
{
    public IReadOnlyCollection<WorkflowTransitionInput> Transitions { get; init; } =
        Array.Empty<WorkflowTransitionInput>();
}

public enum WorkflowConfigurationOutcome
{
    Success,
    NotFound,
    ValidationError,
    Conflict,
    Unauthenticated
}

public sealed record WorkflowConfigurationResult(
    WorkflowConfigurationOutcome Outcome,
    WorkflowConfigurationResponse? Configuration = null,
    string? Error = null);
