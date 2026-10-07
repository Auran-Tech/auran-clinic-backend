namespace Auran.Clinic.Application.PendingDocumentation;

public interface IPendingDocumentationService
{
    Task<IReadOnlyList<PendingDocumentationResponse>> ListAsync(
        PendingDocumentationQuery query,
        CancellationToken cancellationToken = default);

    Task<PendingDocumentationResult> CompleteAsync(
        CompletePendingDocumentationRequest request,
        CancellationToken cancellationToken = default);
}
