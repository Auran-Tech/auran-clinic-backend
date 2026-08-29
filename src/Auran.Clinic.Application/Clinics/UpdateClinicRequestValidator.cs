using Auran.Clinic.Application.ReferenceData;
using FluentValidation;

namespace Auran.Clinic.Application.Clinics;

public sealed class UpdateClinicRequestValidator : AbstractValidator<UpdateClinicRequest>
{
    public UpdateClinicRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PatientNumberPrefix).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
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
            .NotEmpty()
            .MaximumLength(100)
            .Must(ReferenceDataCatalog.IsSupportedTimeZone)
            .WithMessage("TimeZoneId is not supported.");
        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .Length(2)
            .Must(ReferenceDataCatalog.IsSupportedCountry)
            .WithMessage("CountryCode is not supported.");
        RuleFor(x => x.CityCode)
            .NotEmpty()
            .MaximumLength(20)
            .Must((request, cityCode) => ReferenceDataCatalog.IsSupportedCity(request.CountryCode, cityCode))
            .WithMessage("CityCode is not supported for the selected CountryCode.");
        RuleFor(x => x.Locale)
            .NotEmpty()
            .MaximumLength(20)
            .Must(ReferenceDataCatalog.IsSupportedLocale)
            .WithMessage("Locale is not supported.");
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Website).MaximumLength(500);
    }
}
