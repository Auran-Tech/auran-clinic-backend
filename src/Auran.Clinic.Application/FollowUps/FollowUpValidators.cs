using FluentValidation;

namespace Auran.Clinic.Application.FollowUps;

public sealed class CreateFollowUpRequestValidator : AbstractValidator<CreateFollowUpRequest>
{
    public CreateFollowUpRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.Recommendation).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RecommendedAfterDays)
            .GreaterThan(0)
            .When(x => x.RecommendedAfterDays.HasValue);

        RuleFor(x => x)
            .Must(x => x.RecommendedDate.HasValue || x.RecommendedAfterDays.HasValue)
            .WithMessage("A recommended date or recommended number of days is required.");
    }
}

public sealed class ChangeFollowUpStatusRequestValidator : AbstractValidator<ChangeFollowUpStatusRequest>
{
    public ChangeFollowUpStatusRequestValidator() =>
        RuleFor(x => x.FollowUpId).NotEmpty();
}
