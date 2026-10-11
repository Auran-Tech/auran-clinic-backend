using FluentValidation;

namespace Auran.Clinic.Application.Queue;

public sealed class MoveQueueEntryRequestValidator : AbstractValidator<MoveQueueEntryRequest>
{
    public MoveQueueEntryRequestValidator()
    {
        RuleFor(x => x.QueueEntryId).NotEmpty();
        RuleFor(x => x.ToWorkflowStatusId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
