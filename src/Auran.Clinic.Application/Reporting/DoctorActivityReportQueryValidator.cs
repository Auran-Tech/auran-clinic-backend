using FluentValidation;

namespace Auran.Clinic.Application.Reporting;

public sealed class DoctorActivityReportQueryValidator : AbstractValidator<DoctorActivityReportQuery>
{
    public DoctorActivityReportQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => !query.FromDate.HasValue || !query.ToDate.HasValue || query.FromDate <= query.ToDate)
            .WithMessage("FromDate must be on or before ToDate.");
        RuleFor(query => query.ToDate)
            .Must(date => !date.HasValue || date.Value != DateOnly.MaxValue)
            .WithMessage("ToDate is outside the supported range.");
        RuleFor(query => query.DoctorId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("DoctorId must not be empty.");
    }
}
