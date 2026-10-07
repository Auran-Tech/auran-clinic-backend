namespace Auran.Clinic.Application.ClinicalMeasurements;

public interface IClinicalMeasurementService
{
    Task<IReadOnlyList<ClinicalMeasurementFieldResponse>> ListFieldsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClinicalMeasurementResponse>?> ListForVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<ClinicalMeasurementResult> RecordAsync(
        RecordClinicalMeasurementsRequest request,
        CancellationToken cancellationToken = default);
}
