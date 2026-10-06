using FluentValidation;

namespace Auran.Clinic.Application.Visits;

public sealed class StartVisitRequestValidator : AbstractValidator<StartVisitRequest>
{
    public StartVisitRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
    }
}
