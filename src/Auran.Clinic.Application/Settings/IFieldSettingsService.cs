namespace Auran.Clinic.Application.Settings;

public interface IFieldSettingsService
{
    Task<FieldSettingsResponse> GetAsync(CancellationToken cancellationToken = default);
    Task<FieldSettingsResponse?> SaveAsync(
        SaveFieldSettingsRequest request,
        CancellationToken cancellationToken = default);
}
