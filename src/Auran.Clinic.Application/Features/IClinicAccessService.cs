namespace Auran.Clinic.Application.Features;

public interface IClinicAccessService
{
    Task<bool> IsClinicActiveAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<bool> IsFeatureEnabledAsync(Guid clinicId, string featureCode, CancellationToken cancellationToken = default);
    Task InvalidateClinicStatusAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task InvalidateFeatureAsync(Guid clinicId, string featureCode, CancellationToken cancellationToken = default);
}
