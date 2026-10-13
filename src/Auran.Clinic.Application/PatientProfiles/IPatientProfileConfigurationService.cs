namespace Auran.Clinic.Application.PatientProfiles;

public interface IPatientProfileConfigurationService
{
    Task<PatientProfileAdminConfigurationResponse> GetAsync(
        CancellationToken cancellationToken = default);

    Task<PatientProfileConfigurationResult> CreateSectionAsync(
        CreatePatientProfileSectionRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientProfileConfigurationResult> UpdateSectionAsync(
        UpdatePatientProfileSectionRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientProfileConfigurationResult> CreateFieldAsync(
        CreatePatientProfileFieldRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientProfileConfigurationResult> UpdateFieldAsync(
        UpdatePatientProfileFieldRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientProfileConfigurationResult> CreateOptionAsync(
        CreatePatientProfileOptionRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientProfileConfigurationResult> UpdateOptionAsync(
        UpdatePatientProfileOptionRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientProfileConfigurationResult> DeleteOptionAsync(
        DeletePatientProfileOptionRequest request,
        CancellationToken cancellationToken = default);
}
