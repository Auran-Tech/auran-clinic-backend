using FluentValidation;

namespace Auran.Clinic.Application.Patients;

public sealed class PatientQueryValidator : AbstractValidator<PatientQuery>
{
    public PatientQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(256);
        RuleFor(x => x.Gender)
            .Must(value => value is null || PatientListOptions.GenderValues.Contains(value));
        RuleFor(x => x.SortBy)
            .Must(PatientListOptions.SortFields.Contains);
    }
}

public sealed class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Gender).MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.DateOfBirth)
            .Must(value => value is null || value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
    }
}

public sealed class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Gender).MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.DateOfBirth)
            .Must(value => value is null || value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
    }
}
