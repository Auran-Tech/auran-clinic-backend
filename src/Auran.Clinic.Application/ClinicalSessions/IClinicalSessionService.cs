namespace Auran.Clinic.Application.ClinicalSessions;

public interface IClinicalSessionService
{
    Task<ClinicalSessionResponse?> GetActiveAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<ClinicalSessionResult> StartAsync(
        StartClinicalSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<ClinicalSessionResult> SaveDocumentationAsync(
        SaveClinicalDocumentationRequest request,
        CancellationToken cancellationToken = default);

    Task<ClinicalSessionResult> EndAsync(
        EndClinicalSessionRequest request,
        CancellationToken cancellationToken = default);
}
