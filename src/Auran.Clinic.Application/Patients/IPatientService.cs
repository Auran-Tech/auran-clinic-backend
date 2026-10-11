using Auran.Clinic.Application.Models;

namespace Auran.Clinic.Application.Patients;

public interface IPatientService
{
    Task<PaginatedResponse<PatientResponse>> ListAsync(
        PatientQuery query,
        CancellationToken cancellationToken = default);

    Task<PatientResponse?> GetAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientDuplicateCandidateResponse>> FindDuplicatesAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientManagementResult> CreateAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientManagementResult> UpdateAsync(
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default);
}
