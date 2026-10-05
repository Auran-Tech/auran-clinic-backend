namespace Auran.Clinic.Application.Patients;

public interface IPatientDynamicProfileService
{
    Task<PatientDynamicProfileResponse?> GetAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<bool> SaveValueAsync(SavePatientDynamicValueRequest request, CancellationToken cancellationToken = default);
}
