using FluentValidation;

namespace Auran.Clinic.Application.Reporting;

public sealed class VisitReportQueryValidator : AbstractValidator<VisitReportQuery>
{
    private static readonly string[] VisitStatuses = ["Open", "Completed", "Cancelled"];
    private static readonly string[] DocumentationStatuses = ["NotStarted", "Draft", "Pending", "Completed"];

    public VisitReportQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
            .WithMessage("FromDate must be on or before ToDate.");

        RuleFor(x => x.VisitStatus)
            .Must(value => value is null || VisitStatuses.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Unknown visit status.");

        RuleFor(x => x.DocumentationStatus)
            .Must(value => value is null || DocumentationStatuses.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Unknown documentation status.");
    }
}
