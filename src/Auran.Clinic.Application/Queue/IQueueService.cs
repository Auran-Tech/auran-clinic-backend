namespace Auran.Clinic.Application.Queue;

public interface IQueueService
{
    Task<QueueBoardResponse> GetBoardAsync(CancellationToken cancellationToken = default);
    Task<QueueMutationResult> CheckInAsync(QueueCheckInRequest request, CancellationToken cancellationToken = default);
    Task<QueueMutationResult> MoveAsync(QueueMoveRequest request, CancellationToken cancellationToken = default);
}
