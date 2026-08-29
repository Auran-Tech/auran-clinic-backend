using System.ComponentModel.DataAnnotations;

namespace Auran.Clinic.Api.Validation;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DateRangeAttribute(string fromProperty, string toProperty) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        var fromValue = validationContext.ObjectType.GetProperty(fromProperty)?.GetValue(value);
        var toValue = validationContext.ObjectType.GetProperty(toProperty)?.GetValue(value);
        var from = fromValue is DateTime fromDate ? fromDate : (DateTime?)null;
        var to = toValue is DateTime toDate ? toDate : (DateTime?)null;

        return !from.HasValue || !to.HasValue || from.Value <= to.Value
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? $"{fromProperty} must be earlier than or equal to {toProperty}.");
    }
}
