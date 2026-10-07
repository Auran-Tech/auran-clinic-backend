using Auran.Clinic.Domain.Enums;
using FluentValidation;

namespace Auran.Clinic.Application.ClinicalMeasurements;

public sealed class CreateClinicalFieldRequestValidator
    : AbstractValidator<CreateClinicalFieldRequest>
{
    public CreateClinicalFieldRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.FieldType)
            .NotEmpty()
            .Must(value => Enum.TryParse<DynamicFieldType>(value, true, out _))
            .WithMessage("Unknown dynamic field type.");
        RuleFor(x => x.Unit).MaximumLength(50);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateClinicalFieldRequestValidator
    : AbstractValidator<UpdateClinicalFieldRequest>
{
    public UpdateClinicalFieldRequestValidator()
    {
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.FieldType)
            .NotEmpty()
            .Must(value => Enum.TryParse<DynamicFieldType>(value, true, out _))
            .WithMessage("Unknown dynamic field type.");
        RuleFor(x => x.Unit).MaximumLength(50);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateClinicalFieldOptionRequestValidator
    : AbstractValidator<CreateClinicalFieldOptionRequest>
{
    public CreateClinicalFieldOptionRequestValidator()
    {
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(120);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateClinicalFieldOptionRequestValidator
    : AbstractValidator<UpdateClinicalFieldOptionRequest>
{
    public UpdateClinicalFieldOptionRequestValidator()
    {
        RuleFor(x => x.OptionId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(120);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class DeleteClinicalFieldOptionRequestValidator
    : AbstractValidator<DeleteClinicalFieldOptionRequest>
{
    public DeleteClinicalFieldOptionRequestValidator() =>
        RuleFor(x => x.OptionId).NotEmpty();
}
