using System.ComponentModel.DataAnnotations;

namespace Auran.Clinic.Api.Validation;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DateRangeAttribute(string fromProperty, string toProperty) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        var from = validationContext.ObjectType.GetProperty(fromProperty)?.GetValue(value) as DateTime?;
        var to = validationContext.ObjectType.GetProperty(toProperty)?.GetValue(value) as DateTime?;

        return !from.HasValue || !to.HasValue || from.Value <= to.Value
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? $"{fromProperty} must be earlier than or equal to {toProperty}.");
    }
}
