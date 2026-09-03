namespace Auran.Clinic.Application.Clinics;

public interface IPlatformClinicService
{
    Task<ClinicProvisioningResult> CreateAsync(
        CreateClinicRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClinicSummaryResponse>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<ClinicDetailsResponse?> GetAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default);

    Task<ClinicDetailsResponse?> UpdateAsync(
        Guid clinicId,
        UpdateClinicRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(
        Guid clinicId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
