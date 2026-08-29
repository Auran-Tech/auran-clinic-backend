using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Auran.Clinic.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CodePrefixAttribute : ValidationAttribute
{
    private static readonly Regex Pattern = new("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || value is string { Length: 0 })
            return ValidationResult.Success;

        if (value is not string text || string.IsNullOrWhiteSpace(text))
            return ValidationResult.Success;

        return text.Length <= 20 && Pattern.IsMatch(text)
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must be at most 20 characters and contain letters, numbers, '_' or '-' only.");
    }
}
