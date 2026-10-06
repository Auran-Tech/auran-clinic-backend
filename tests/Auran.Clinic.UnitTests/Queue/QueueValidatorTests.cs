using Auran.Clinic.Application.Queue;

namespace Auran.Clinic.UnitTests.Queue;

public sealed class QueueValidatorTests
{
    [Fact]
    public void Move_requires_queue_entry_and_target_status()
    {
        var validator = new MoveQueueEntryRequestValidator();

        var result = validator.Validate(new MoveQueueEntryRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(MoveQueueEntryRequest.QueueEntryId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(MoveQueueEntryRequest.ToWorkflowStatusId));
    }

    [Fact]
    public void Move_rejects_notes_over_limit()
    {
        var validator = new MoveQueueEntryRequestValidator();

        var result = validator.Validate(new MoveQueueEntryRequest
        {
            QueueEntryId = Guid.NewGuid(),
            ToWorkflowStatusId = Guid.NewGuid(),
            Notes = new string('A', 1001)
        });

        Assert.False(result.IsValid);
    }
}
