namespace Auran.Clinic.Application.PatientProfiles;

public interface IPatientProfileService
{
    Task<PatientProfileConfigurationResponse> GetConfigurationAsync(
        CancellationToken cancellationToken = default);

    Task<PatientProfileResponse?> GetPatientProfileAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientProfileResult> SaveAsync(
        SavePatientProfileRequest request,
        CancellationToken cancellationToken = default);
}
