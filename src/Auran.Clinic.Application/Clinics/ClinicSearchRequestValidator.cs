using FluentValidation;

namespace Auran.Clinic.Application.Clinics;

public sealed class ClinicSearchRequestValidator : AbstractValidator<ClinicSearchRequest>
{
    public ClinicSearchRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
