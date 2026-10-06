namespace Auran.Clinic.Application.Queue;

public interface IQueueService
{
    Task<IReadOnlyList<QueueEntryResponse>> ListActiveAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QueueTransitionOptionResponse>> GetAvailableTransitionsAsync(
        Guid queueEntryId,
        CancellationToken cancellationToken = default);

    Task<QueueMoveResult> MoveAsync(
        MoveQueueEntryRequest request,
        CancellationToken cancellationToken = default);
}
