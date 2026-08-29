using System.ComponentModel.DataAnnotations;
using Auran.Clinic.Application.ReferenceData;

namespace Auran.Clinic.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SupportedReferenceAttribute(ReferenceDataKind kind) : ValidationAttribute
{
    public string? RelatedProperty { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
            return ValidationResult.Success;
        if (value is not string candidate)
            return Invalid(validationContext);

        var valid = kind switch
        {
            ReferenceDataKind.Font => ReferenceDataCatalog.IsSupportedFont(candidate),
            ReferenceDataKind.TimeZone => ReferenceDataCatalog.IsSupportedTimeZone(candidate),
            ReferenceDataKind.Country => ReferenceDataCatalog.IsSupportedCountry(candidate),
            ReferenceDataKind.City => ReferenceDataCatalog.IsSupportedCity(GetRelatedValue(validationContext), candidate),
            ReferenceDataKind.Locale => ReferenceDataCatalog.IsSupportedLocale(candidate),
            ReferenceDataKind.DateFormat => ReferenceDataCatalog.IsSupportedDateFormat(candidate),
            ReferenceDataKind.TimeFormat => ReferenceDataCatalog.IsSupportedTimeFormat(candidate),
            _ => false
        };

        return valid ? ValidationResult.Success : Invalid(validationContext);
    }

    private string? GetRelatedValue(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(RelatedProperty))
            return null;

        return context.ObjectType.GetProperty(RelatedProperty)?.GetValue(context.ObjectInstance) as string;
    }

    private ValidationResult Invalid(ValidationContext context) =>
        new(ErrorMessage ?? $"{context.DisplayName} is not a supported {kind} value.");
}
