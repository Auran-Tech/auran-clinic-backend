using FluentValidation;

namespace Auran.Clinic.Application.Clinics;

public sealed class CreateClinicRequestValidator : AbstractValidator<CreateClinicRequest>
{
    public CreateClinicRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.PatientNumberPrefix).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.PrimaryColor).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.PrimaryColor));
        RuleFor(x => x.SecondaryColor).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.SecondaryColor));
        RuleFor(x => x.TimeZoneId).MaximumLength(100);
        RuleFor(x => x.Locale).MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Admin).NotNull();
        RuleFor(x => x.Admin.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Admin.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Admin.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Admin password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Admin password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Admin password must contain a digit.");
    }
}
