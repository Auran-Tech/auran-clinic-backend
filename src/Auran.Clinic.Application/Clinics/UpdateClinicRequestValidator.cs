using FluentValidation;

namespace Auran.Clinic.Application.Clinics;

public sealed class UpdateClinicRequestValidator : AbstractValidator<UpdateClinicRequest>
{
    public UpdateClinicRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.PatientNumberPrefix).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.LogoUrl).MaximumLength(500);
        RuleFor(x => x.PrimaryColor).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.PrimaryColor));
        RuleFor(x => x.SecondaryColor).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.SecondaryColor));
        RuleFor(x => x.FontFamily).MaximumLength(100);
        RuleFor(x => x.WelcomeTitle).MaximumLength(200);
        RuleFor(x => x.WelcomeMessage).MaximumLength(2000);
        RuleFor(x => x.TimeZoneId).MaximumLength(100);
    }
}
