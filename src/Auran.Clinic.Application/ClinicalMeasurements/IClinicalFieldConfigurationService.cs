namespace Auran.Clinic.Application.ClinicalMeasurements;

public interface IClinicalFieldConfigurationService
{
    Task<ClinicalFieldAdminConfigurationResponse> GetAsync(
        CancellationToken cancellationToken = default);

    Task<ClinicalFieldConfigurationResult> CreateFieldAsync(
        CreateClinicalFieldRequest request,
        CancellationToken cancellationToken = default);

    Task<ClinicalFieldConfigurationResult> UpdateFieldAsync(
        UpdateClinicalFieldRequest request,
        CancellationToken cancellationToken = default);

    Task<ClinicalFieldConfigurationResult> CreateOptionAsync(
        CreateClinicalFieldOptionRequest request,
        CancellationToken cancellationToken = default);

    Task<ClinicalFieldConfigurationResult> UpdateOptionAsync(
        UpdateClinicalFieldOptionRequest request,
        CancellationToken cancellationToken = default);

    Task<ClinicalFieldConfigurationResult> DeleteOptionAsync(
        DeleteClinicalFieldOptionRequest request,
        CancellationToken cancellationToken = default);
}
