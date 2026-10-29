using FluentValidation;

namespace Auran.Clinic.Application.Visits;

public sealed class VisitListQueryValidator : AbstractValidator<VisitListQuery>
{
    private static readonly string[] SortFields =
    [
        "entryAtUtc",
        "patientName",
        "doctorName",
        "status"
    ];

    public VisitListQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.Status).IsInEnum();
        RuleFor(query => query.DocumentationStatus).IsInEnum();
        RuleFor(query => query.SortBy).Must(value =>
                SortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Unsupported visit sort field.");
        RuleFor(query => query).Must(query =>
                ((long)query.Page - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("Requested page exceeds the supported pagination range.");
        RuleFor(query => query).Must(query =>
                !query.FromDate.HasValue ||
                !query.ToDate.HasValue ||
                query.FromDate <= query.ToDate)
            .WithMessage("FromDate must be before or equal to ToDate.");
        RuleFor(query => query.ToDate)
            .Must(value => !value.HasValue || value.Value < DateOnly.MaxValue)
            .WithMessage("ToDate is outside the supported range.");
    }
}
