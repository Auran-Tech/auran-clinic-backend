using Auran.Clinic.Application.Models;

namespace Auran.Clinic.Application.Patients;

public interface IPatientService
{
    Task<PaginatedResponse<PatientResponse>> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PatientResponse?> GetAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<PatientDuplicateCheckResponse> CheckDuplicatesAsync(
        PatientDuplicateCheckRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientMutationResult> CreateAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default);

    Task<PatientMutationResult> UpdateAsync(
        Guid patientId,
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default);
}
