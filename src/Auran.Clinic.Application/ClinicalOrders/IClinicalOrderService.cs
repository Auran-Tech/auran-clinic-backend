namespace Auran.Clinic.Application.ClinicalOrders;

public interface IClinicalOrderService
{
    Task<IReadOnlyCollection<ClinicalOrderSectionDefinitionResponse>> GetDefinitionsAsync(
        CancellationToken cancellationToken = default);

    Task<ClinicalOrderResponse?> GetByVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<ClinicalOrderResponse?> SaveAsync(
        SaveClinicalOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyCollection<ClinicalOrderSectionDefinitionResponse>? Definitions, string? Error)> SaveDefinitionsAsync(
        SaveClinicalOrderSectionDefinitionsRequest request,
        CancellationToken cancellationToken = default);
}
