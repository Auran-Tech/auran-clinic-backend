namespace Auran.Clinic.Application.Settings;

public interface IWorkflowSettingsService
{
    Task<WorkflowSettingsResponse> GetAsync(CancellationToken cancellationToken = default);
    Task<(WorkflowSettingsResponse? Settings, string? Error)> SaveAsync(
        SaveWorkflowSettingsRequest request,
        CancellationToken cancellationToken = default);
}
