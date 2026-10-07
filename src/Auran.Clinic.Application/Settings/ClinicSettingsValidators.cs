using FluentValidation;

namespace Auran.Clinic.Application.Settings;

public sealed class UpdateClinicSettingsRequestValidator
    : AbstractValidator<UpdateClinicSettingsRequest>
{
    public UpdateClinicSettingsRequestValidator()
    {
        RuleFor(x => x.ClinicName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LogoUrl).MaximumLength(2000);
        RuleFor(x => x.PrimaryColor).MaximumLength(20);
        RuleFor(x => x.SecondaryColor).MaximumLength(20);
        RuleFor(x => x.FontFamily).MaximumLength(100);
        RuleFor(x => x.TimeZoneId).MaximumLength(100);
        RuleFor(x => x.PatientNumberPrefix).MaximumLength(20);
        RuleFor(x => x.Phone).MaximumLength(64);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.Website).MaximumLength(2000);
        RuleFor(x => x.Locale).MaximumLength(32);
        RuleFor(x => x.DateFormat).MaximumLength(64);
        RuleFor(x => x.TimeFormat).MaximumLength(64);
        RuleFor(x => x.DocumentationReminderHours).InclusiveBetween(1, 720);
    }
}
