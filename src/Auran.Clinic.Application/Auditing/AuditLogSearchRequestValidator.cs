using FluentValidation;

namespace Auran.Clinic.Application.Auditing;

public sealed class AuditLogSearchRequestValidator : AbstractValidator<AuditLogSearchRequest>
{
    public AuditLogSearchRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.Action).MaximumLength(100);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.EntityType).MaximumLength(200);
        RuleFor(x => x.EntityId).MaximumLength(200);
        RuleFor(x => x.ToUtc)
            .GreaterThanOrEqualTo(x => x.FromUtc!.Value)
            .When(x => x.FromUtc.HasValue && x.ToUtc.HasValue);
    }
}
