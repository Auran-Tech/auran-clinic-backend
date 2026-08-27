using Auran.Clinic.Application.Models;

namespace Auran.Clinic.Application.Clinics;

public interface IClinicService
{
    Task<ClinicProvisioningResult> CreateAsync(CreateClinicRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<ClinicSummaryResponse>> SearchAsync(ClinicSearchRequest request, CancellationToken cancellationToken = default);
    Task<ClinicDetailsResponse?> GetByIdAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid clinicId, UpdateClinicRequest request, CancellationToken cancellationToken = default);
    Task<bool> SetActiveAsync(Guid clinicId, bool isActive, CancellationToken cancellationToken = default);
    Task<ClinicSettingsResponse?> GetSettingsAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<ClinicSettingsResponse?> UpdateSettingsAsync(Guid clinicId, UpdateClinicSettingsRequest request, CancellationToken cancellationToken = default);
}
