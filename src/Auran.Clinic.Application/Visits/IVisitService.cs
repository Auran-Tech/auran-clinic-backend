namespace Auran.Clinic.Application.Visits;

public interface IVisitService
{
    Task<IReadOnlyCollection<VisitSummaryResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<VisitDetailsResponse?> GetAsync(Guid visitId, CancellationToken cancellationToken = default);
    Task<VisitMutationResult> StartSessionAsync(StartVisitSessionRequest request, CancellationToken cancellationToken = default);
    Task<VisitMutationResult> EndSessionAsync(EndVisitSessionRequest request, CancellationToken cancellationToken = default);
    Task<VisitMutationResult> SaveDraftAsync(SaveVisitDraftRequest request, CancellationToken cancellationToken = default);
}
