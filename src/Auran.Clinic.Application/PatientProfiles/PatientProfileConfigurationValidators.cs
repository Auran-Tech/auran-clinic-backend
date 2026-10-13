using Auran.Clinic.Domain.Enums;
using FluentValidation;

namespace Auran.Clinic.Application.PatientProfiles;

public sealed class CreatePatientProfileSectionRequestValidator
    : AbstractValidator<CreatePatientProfileSectionRequest>
{
    public CreatePatientProfileSectionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdatePatientProfileSectionRequestValidator
    : AbstractValidator<UpdatePatientProfileSectionRequest>
{
    public UpdatePatientProfileSectionRequestValidator()
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreatePatientProfileFieldRequestValidator
    : AbstractValidator<CreatePatientProfileFieldRequest>
{
    public CreatePatientProfileFieldRequestValidator()
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(120);
        RuleFor(x => x.FieldType)
            .NotEmpty()
            .Must(value => Enum.TryParse<DynamicFieldType>(value, true, out _))
            .WithMessage("Unknown dynamic field type.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdatePatientProfileFieldRequestValidator
    : AbstractValidator<UpdatePatientProfileFieldRequest>
{
    public UpdatePatientProfileFieldRequestValidator()
    {
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(120);
        RuleFor(x => x.FieldType)
            .NotEmpty()
            .Must(value => Enum.TryParse<DynamicFieldType>(value, true, out _))
            .WithMessage("Unknown dynamic field type.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreatePatientProfileOptionRequestValidator
    : AbstractValidator<CreatePatientProfileOptionRequest>
{
    public CreatePatientProfileOptionRequestValidator()
    {
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(120);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdatePatientProfileOptionRequestValidator
    : AbstractValidator<UpdatePatientProfileOptionRequest>
{
    public UpdatePatientProfileOptionRequestValidator()
    {
        RuleFor(x => x.OptionId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(120);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class DeletePatientProfileOptionRequestValidator
    : AbstractValidator<DeletePatientProfileOptionRequest>
{
    public DeletePatientProfileOptionRequestValidator() =>
        RuleFor(x => x.OptionId).NotEmpty();
}
