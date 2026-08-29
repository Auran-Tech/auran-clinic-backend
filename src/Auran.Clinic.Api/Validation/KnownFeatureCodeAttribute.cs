using System.ComponentModel.DataAnnotations;
using Auran.Clinic.Application.Features;

namespace Auran.Clinic.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class KnownFeatureCodeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
            return ValidationResult.Success;

        var valid = value is string code && SystemFeatureCatalog.All.Any(x =>
            x.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));

        return valid
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is not a known feature code.");
    }
}
