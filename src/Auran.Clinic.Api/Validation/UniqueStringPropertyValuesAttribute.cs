using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Auran.Clinic.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class UniqueStringPropertyValuesAttribute(string propertyName) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;
        if (value is not IEnumerable items)
            return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} must be a collection.");

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (item is null)
                continue;

            var property = item.GetType().GetProperty(propertyName);
            if (property is null)
                return new ValidationResult($"Property '{propertyName}' was not found on {item.GetType().Name}.");

            if (property.GetValue(item) is not string text || string.IsNullOrWhiteSpace(text))
                continue;

            if (!values.Add(text.Trim()))
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} contains duplicate {propertyName} values.");
        }

        return ValidationResult.Success;
    }
}
