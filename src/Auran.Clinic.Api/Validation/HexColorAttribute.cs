using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Auran.Clinic.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class HexColorAttribute : ValidationAttribute
{
    private static readonly Regex Pattern = new("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
            return ValidationResult.Success;

        return value is string color && Pattern.IsMatch(color)
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must use #RRGGBB format.");
    }
}
