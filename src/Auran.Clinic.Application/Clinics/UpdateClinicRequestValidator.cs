using FluentValidation;

namespace Auran.Clinic.Application.Clinics;

public sealed class UpdateClinicRequestValidator : AbstractValidator<UpdateClinicRequest>
{
    public UpdateClinicRequestValidator()
    {
        RuleFor(request => request.ClinicId).NotEmpty();
    }
}
