using FluentValidation;

namespace Auran.Clinic.Application.Clinics;

public sealed class SetClinicStatusRequestValidator : AbstractValidator<SetClinicStatusRequest>
{
    public SetClinicStatusRequestValidator()
    {
        RuleFor(request => request.ClinicId).NotEmpty();
    }
}
