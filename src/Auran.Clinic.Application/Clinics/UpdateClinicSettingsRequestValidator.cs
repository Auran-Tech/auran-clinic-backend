using FluentValidation;

namespace Auran.Clinic.Application.Clinics;

public sealed class UpdateClinicSettingsRequestValidator : AbstractValidator<UpdateClinicSettingsRequest>
{
    public UpdateClinicSettingsRequestValidator()
    {
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Website).MaximumLength(500);
        RuleFor(x => x.Locale).MaximumLength(20);
        RuleFor(x => x.DateFormat).MaximumLength(50);
        RuleFor(x => x.TimeFormat).MaximumLength(50);
        RuleFor(x => x.DocumentationReminderHours).InclusiveBetween(1, 168);
        RuleFor(x => x.WelcomeButtonText).MaximumLength(100);
    }
}
