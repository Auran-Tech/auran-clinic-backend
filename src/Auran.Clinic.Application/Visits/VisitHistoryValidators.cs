using FluentValidation;

namespace Auran.Clinic.Application.Visits;

public sealed class PatientVisitHistoryQueryValidator
    : AbstractValidator<PatientVisitHistoryQuery>
{
    public PatientVisitHistoryQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
