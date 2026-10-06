using FluentValidation;

namespace Auran.Clinic.Application.Patients;

public sealed class PatientDuplicateCheckRequestValidator : AbstractValidator<PatientDuplicateCheckRequest>
{
    public PatientDuplicateCheckRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.DateOfBirth.HasValue);
    }
}
