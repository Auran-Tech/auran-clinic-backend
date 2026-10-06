namespace Auran.Clinic.Application.Patients;

public interface IPatientClinicalProfileService
{
    Task<PatientClinicalProfileResponse?> GetAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientAllergyResponse?> AddAllergyAsync(
        AddPatientAllergyRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientConditionResponse?> AddConditionAsync(
        AddPatientConditionRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientMedicationResponse?> AddMedicationAsync(
        AddPatientMedicationRequest request,
        CancellationToken cancellationToken = default);
}
