using FluentValidation;

namespace Auran.Clinic.Application.ClinicalMeasurements;

public sealed class RecordClinicalMeasurementsRequestValidator
    : AbstractValidator<RecordClinicalMeasurementsRequest>
{
    public RecordClinicalMeasurementsRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.Values).NotEmpty();

        RuleForEach(x => x.Values).ChildRules(value =>
        {
            value.RuleFor(x => x.FieldId).NotEmpty();
            value.RuleFor(x => x.TextValue).MaximumLength(8000);
            value.RuleFor(x => x.JsonValue).MaximumLength(16000);
        });
    }
}
