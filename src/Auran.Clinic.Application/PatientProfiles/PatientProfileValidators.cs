using FluentValidation;

namespace Auran.Clinic.Application.PatientProfiles;

public sealed class SavePatientProfileRequestValidator
    : AbstractValidator<SavePatientProfileRequest>
{
    public SavePatientProfileRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();

        RuleForEach(x => x.Values).ChildRules(value =>
        {
            value.RuleFor(x => x.FieldId).NotEmpty();
            value.RuleFor(x => x.TextValue).MaximumLength(8000);
            value.RuleFor(x => x.JsonValue).MaximumLength(16000);
        });
    }
}
