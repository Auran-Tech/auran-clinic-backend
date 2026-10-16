using FluentValidation;

namespace Auran.Clinic.Application.Reporting;

public sealed class PatientReportQueryValidator : AbstractValidator<PatientReportQuery>
{
    public PatientReportQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
            .WithMessage("FromDate must be on or before ToDate.");
        RuleFor(x => x.Gender).MaximumLength(32);
    }
}
