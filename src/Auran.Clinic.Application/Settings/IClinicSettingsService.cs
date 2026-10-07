namespace Auran.Clinic.Application.Settings;

public interface IClinicSettingsService
{
    Task<ClinicSettingsResponse?> GetAsync(
        CancellationToken cancellationToken = default);

    Task<ClinicSettingsResult> UpdateAsync(
        UpdateClinicSettingsRequest request,
        CancellationToken cancellationToken = default);
}
