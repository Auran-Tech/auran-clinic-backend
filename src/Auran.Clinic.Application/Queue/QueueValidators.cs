using FluentValidation;

namespace Auran.Clinic.Application.Queue;

public sealed class QueueQueryValidator : AbstractValidator<QueueQuery>
{
    public QueueQueryValidator()
    {
        RuleFor(x => x.SortBy)
            .Must(QueueListOptions.SortFields.Contains);
    }
}

public sealed class MoveQueueEntryRequestValidator : AbstractValidator<MoveQueueEntryRequest>
{
    public MoveQueueEntryRequestValidator()
    {
        RuleFor(x => x.QueueEntryId).NotEmpty();
        RuleFor(x => x.ToWorkflowStatusId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
