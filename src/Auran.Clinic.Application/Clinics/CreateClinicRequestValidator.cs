using FluentValidation;

namespace Auran.Clinic.Application.Clinics;

public sealed class CreateClinicRequestValidator : AbstractValidator<CreateClinicRequest>
{
    public CreateClinicRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CodePrefix).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.PatientNumberPrefix).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.LogoUrl).MaximumLength(500);
        RuleFor(x => x.PrimaryColor).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.PrimaryColor));
        RuleFor(x => x.SecondaryColor).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.SecondaryColor));
        RuleFor(x => x.FontFamily).MaximumLength(100);
        RuleFor(x => x.WelcomeTitle).MaximumLength(200);
        RuleFor(x => x.WelcomeMessage).MaximumLength(2000);
        RuleFor(x => x.WelcomeButtonText).MaximumLength(100);
        RuleFor(x => x.TimeZoneId).MaximumLength(100);
        RuleFor(x => x.Locale).MaximumLength(20);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Website).MaximumLength(500);
        RuleFor(x => x.Admin).NotNull();
        RuleFor(x => x.Admin.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Admin.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Admin.Phone).MaximumLength(50);
        RuleFor(x => x.Admin.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Admin password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Admin password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Admin password must contain a digit.");
    }
}
