using FluentValidation;

namespace Auran.Clinic.Application.Auditing;

public sealed class AuditLogSearchRequestValidator : AbstractValidator<AuditLogSearchRequest>
{
    public AuditLogSearchRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.Action).MaximumLength(160);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.EntityType).MaximumLength(160);
        RuleFor(x => x.EntityId).MaximumLength(100);
        RuleFor(x => x)
            .Must(x => !x.FromUtc.HasValue || !x.ToUtc.HasValue || x.FromUtc <= x.ToUtc)
            .WithMessage("FromUtc must be earlier than or equal to ToUtc.");
    }
}
