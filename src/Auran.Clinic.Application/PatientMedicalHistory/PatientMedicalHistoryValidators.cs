using FluentValidation;

namespace Auran.Clinic.Application.PatientMedicalHistory;

public sealed class CreatePatientConditionRequestValidator
    : AbstractValidator<CreatePatientConditionRequest>
{
    public CreatePatientConditionRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}

public sealed class CreatePatientAllergyRequestValidator
    : AbstractValidator<CreatePatientAllergyRequest>
{
    public CreatePatientAllergyRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reaction).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}

public sealed class CreatePatientMedicationRequestValidator
    : AbstractValidator<CreatePatientMedicationRequest>
{
    public CreatePatientMedicationRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dosage).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}

public sealed class DeletePatientMedicalHistoryItemRequestValidator
    : AbstractValidator<DeletePatientMedicalHistoryItemRequest>
{
    public DeletePatientMedicalHistoryItemRequestValidator() =>
        RuleFor(x => x.ItemId).NotEmpty();
}
