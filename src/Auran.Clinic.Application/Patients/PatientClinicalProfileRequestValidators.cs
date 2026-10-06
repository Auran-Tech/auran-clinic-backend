using FluentValidation;

namespace Auran.Clinic.Application.Patients;

public sealed class AddPatientAllergyRequestValidator : AbstractValidator<AddPatientAllergyRequest>
{
    public AddPatientAllergyRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Reaction).MaximumLength(1000);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}

public sealed class AddPatientConditionRequestValidator : AbstractValidator<AddPatientConditionRequest>
{
    public AddPatientConditionRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}

public sealed class AddPatientMedicationRequestValidator : AbstractValidator<AddPatientMedicationRequest>
{
    public AddPatientMedicationRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Dosage).MaximumLength(256);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
