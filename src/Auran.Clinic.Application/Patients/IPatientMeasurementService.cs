namespace Auran.Clinic.Application.Patients;

public interface IPatientMeasurementService
{
    Task<PatientMeasurementsResponse?> GetAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<ClinicalMeasurementResponse?> AddAsync(AddClinicalMeasurementRequest request, CancellationToken cancellationToken = default);
}
