namespace Auran.Clinic.Application.ClinicalOrders;

public interface IClinicalOrderService
{
    Task<IReadOnlyList<ClinicalOrderSectionDefinitionResponse>> ListDefinitionsAsync(
        CancellationToken cancellationToken = default);

    Task<ClinicalOrderResponse?> GetForVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<ClinicalOrderResult> SaveAsync(
        SaveClinicalOrderRequest request,
        CancellationToken cancellationToken = default);
}
