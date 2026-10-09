namespace Auran.Clinic.Application.ClinicalOrders;

public interface IClinicalOrderSectionConfigurationService
{
    Task<ClinicalOrderSectionAdminConfigurationResponse> GetAsync(
        CancellationToken cancellationToken = default);

    Task<ClinicalOrderSectionConfigurationResult> CreateAsync(
        CreateClinicalOrderSectionDefinitionRequest request,
        CancellationToken cancellationToken = default);

    Task<ClinicalOrderSectionConfigurationResult> UpdateAsync(
        UpdateClinicalOrderSectionDefinitionRequest request,
        CancellationToken cancellationToken = default);
}
