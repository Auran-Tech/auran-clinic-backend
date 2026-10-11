namespace Auran.Clinic.Application.FollowUps;

public interface IFollowUpService
{
    Task<IReadOnlyList<FollowUpResponse>> ListAsync(
        FollowUpQuery query,
        CancellationToken cancellationToken = default);

    Task<FollowUpResult> CreateAsync(
        CreateFollowUpRequest request,
        CancellationToken cancellationToken = default);

    Task<FollowUpResult> CompleteAsync(
        ChangeFollowUpStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<FollowUpResult> CancelAsync(
        ChangeFollowUpStatusRequest request,
        CancellationToken cancellationToken = default);
}
