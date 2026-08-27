using Auran.Clinic.Application.Features;

namespace Auran.Clinic.Application.Clinics;

public interface IClinicService
{
    Task<ClinicDetailsResponse?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<ClinicSettingsResponse?> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<ClinicSettingsResponse?> UpdateSettingsAsync(UpdateClinicSettingsRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ClinicFeatureResponse>?> GetFeaturesAsync(CancellationToken cancellationToken = default);
}
