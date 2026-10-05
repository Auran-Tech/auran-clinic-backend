using Auran.Clinic.Domain.Enums;
using FluentValidation;

namespace Auran.Clinic.Application.ClinicalOrders;

public sealed class SaveClinicalOrderSectionDefinitionsRequest
{
    public IReadOnlyCollection<SaveClinicalOrderSectionDefinitionRequest> Sections { get; init; } = [];
}

public sealed class SaveClinicalOrderSectionDefinitionRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public ClinicalOrderSectionType SectionType { get; init; }
    public int SortOrder { get; init; }
    public bool IsEnabled { get; init; } = true;
}

public sealed class SaveClinicalOrderSectionDefinitionsRequestValidator : AbstractValidator<SaveClinicalOrderSectionDefinitionsRequest>
{
    public SaveClinicalOrderSectionDefinitionsRequestValidator()
    {
        RuleForEach(x => x.Sections).ChildRules(section =>
        {
            section.RuleFor(x => x.Code).NotEmpty().MaximumLength(64).Matches("^[A-Za-z0-9_-]+$");
            section.RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        });
        RuleFor(x => x.Sections)
            .Must(items => items.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == items.Count)
            .WithMessage("Clinical order section codes must be unique.");
    }
}
