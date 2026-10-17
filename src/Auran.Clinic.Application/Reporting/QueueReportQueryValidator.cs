using FluentValidation;

namespace Auran.Clinic.Application.Reporting;

public sealed class QueueReportQueryValidator : AbstractValidator<QueueReportQuery>
{
    public QueueReportQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
            .WithMessage("FromDate must be on or before ToDate.");
        RuleFor(x => x.FromDate)
            .Must(value => !value.HasValue || value.Value != DateOnly.MaxValue)
            .WithMessage("FromDate is outside the supported range.");
        RuleFor(x => x.ToDate)
            .Must(value => !value.HasValue || value.Value != DateOnly.MaxValue)
            .WithMessage("ToDate is outside the supported range.");
    }
}
