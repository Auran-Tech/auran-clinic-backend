namespace Auran.Clinic.Application.FollowUps;

public interface IFollowUpService
{
    Task<IReadOnlyCollection<FollowUpResponse>> ListAsync(
        string? search,
        string? dueCategory,
        CancellationToken cancellationToken = default);

    Task<FollowUpMutationResult> CreateAsync(
        CreateFollowUpRequest request,
        CancellationToken cancellationToken = default);

    Task<FollowUpMutationResult> UpdateAsync(
        UpdateFollowUpRequest request,
        CancellationToken cancellationToken = default);

    Task<FollowUpMutationResult> SetStatusAsync(
        SetFollowUpStatusRequest request,
        CancellationToken cancellationToken = default);
}
