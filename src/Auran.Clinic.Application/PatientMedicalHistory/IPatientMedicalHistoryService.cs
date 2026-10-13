namespace Auran.Clinic.Application.PatientMedicalHistory;

public interface IPatientMedicalHistoryService
{
    Task<PatientMedicalHistoryResponse?> GetAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientMedicalHistoryResult> AddConditionAsync(
        CreatePatientConditionRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientMedicalHistoryResult> DeleteConditionAsync(
        DeletePatientMedicalHistoryItemRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientMedicalHistoryResult> AddAllergyAsync(
        CreatePatientAllergyRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientMedicalHistoryResult> DeleteAllergyAsync(
        DeletePatientMedicalHistoryItemRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientMedicalHistoryResult> AddMedicationAsync(
        CreatePatientMedicationRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientMedicalHistoryResult> DeleteMedicationAsync(
        DeletePatientMedicalHistoryItemRequest request,
        CancellationToken cancellationToken = default);
}
