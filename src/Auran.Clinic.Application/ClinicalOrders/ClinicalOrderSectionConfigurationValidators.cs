using Auran.Clinic.Domain.Enums;
using FluentValidation;

namespace Auran.Clinic.Application.ClinicalOrders;

public sealed class CreateClinicalOrderSectionDefinitionRequestValidator
    : AbstractValidator<CreateClinicalOrderSectionDefinitionRequest>
{
    public CreateClinicalOrderSectionDefinitionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.SectionType)
            .NotEmpty()
            .Must(value => Enum.TryParse<ClinicalOrderSectionType>(value, true, out _))
            .WithMessage("Unknown clinical order section type.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateClinicalOrderSectionDefinitionRequestValidator
    : AbstractValidator<UpdateClinicalOrderSectionDefinitionRequest>
{
    public UpdateClinicalOrderSectionDefinitionRequestValidator()
    {
        RuleFor(x => x.SectionDefinitionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.SectionType)
            .NotEmpty()
            .Must(value => Enum.TryParse<ClinicalOrderSectionType>(value, true, out _))
            .WithMessage("Unknown clinical order section type.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
