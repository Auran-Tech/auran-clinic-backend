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
        RuleFor(x => x).Must(query =>
                ((long)query.Page - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("Requested page exceeds the supported pagination range.");
    }
}
