using Auran.Clinic.Application.ReferenceData;
using FluentValidation;

namespace Auran.Clinic.Application.Clinics;

public sealed class UpdateClinicSettingsRequestValidator : AbstractValidator<UpdateClinicSettingsRequest>
{
    public UpdateClinicSettingsRequestValidator()
    {
        RuleFor(x => x.LogoUrl).MaximumLength(1000);
        RuleFor(x => x.PrimaryColor).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.PrimaryColor));
        RuleFor(x => x.SecondaryColor).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.SecondaryColor));
        RuleFor(x => x.FontFamily)
            .MaximumLength(100)
            .Must(ReferenceDataCatalog.IsSupportedFont)
            .WithMessage("FontFamily is not supported.");
        RuleFor(x => x.WelcomeTitle).MaximumLength(200);
        RuleFor(x => x.WelcomeMessage).MaximumLength(2000);
        RuleFor(x => x.WelcomeButtonText).MaximumLength(100);
        RuleFor(x => x.TimeZoneId)
            .MaximumLength(100)
            .Must(ReferenceDataCatalog.IsSupportedTimeZone)
            .WithMessage("TimeZoneId is not supported.");
        RuleFor(x => x.CountryCode)
            .MaximumLength(2)
            .Must(ReferenceDataCatalog.IsSupportedCountry)
            .WithMessage("CountryCode is not supported.");
        RuleFor(x => x.CityCode)
            .MaximumLength(20)
            .Must((request, cityCode) => ReferenceDataCatalog.IsSupportedCity(request.CountryCode, cityCode))
            .WithMessage("CityCode is not supported for the selected CountryCode.");
        RuleFor(x => x.PatientNumberPrefix)
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9_-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.PatientNumberPrefix));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Website).MaximumLength(500);
        RuleFor(x => x.Locale)
            .MaximumLength(20)
            .Must(ReferenceDataCatalog.IsSupportedLocale)
            .WithMessage("Locale is not supported.");
        RuleFor(x => x.DateFormat)
            .MaximumLength(50)
            .Must(ReferenceDataCatalog.IsSupportedDateFormat)
            .WithMessage("DateFormat is not supported.");
        RuleFor(x => x.TimeFormat)
            .MaximumLength(50)
            .Must(ReferenceDataCatalog.IsSupportedTimeFormat)
            .WithMessage("TimeFormat is not supported.");
        RuleFor(x => x.DocumentationReminderHours).InclusiveBetween(1, 168);
        RuleFor(x => x.PrescriptionHeader).MaximumLength(2000);
        RuleFor(x => x.PrescriptionFooter).MaximumLength(2000);
    }
}
