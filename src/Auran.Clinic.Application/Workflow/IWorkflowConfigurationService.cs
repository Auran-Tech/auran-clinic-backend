namespace Auran.Clinic.Application.Workflow;

public interface IWorkflowConfigurationService
{
    Task<WorkflowConfigurationResponse> GetAsync(
        CancellationToken cancellationToken = default);

    Task<WorkflowConfigurationResult> CreateStatusAsync(
        CreateWorkflowStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkflowConfigurationResult> UpdateStatusAsync(
        UpdateWorkflowStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkflowConfigurationResult> DeleteStatusAsync(
        DeleteWorkflowStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkflowConfigurationResult> ReplaceTransitionsAsync(
        ReplaceWorkflowTransitionsRequest request,
        CancellationToken cancellationToken = default);
}
