using FluentValidation;

namespace Auran.Clinic.Application.ClinicalOrders;

public sealed class SaveClinicalOrderRequestValidator : AbstractValidator<SaveClinicalOrderRequest>
{
    public SaveClinicalOrderRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleForEach(x => x.Sections).ChildRules(section =>
        {
            section.RuleFor(x => x.SectionDefinitionId).NotEmpty();
            section.RuleFor(x => x.TextValue).MaximumLength(8000);
            section.RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.Name).NotEmpty().MaximumLength(512);
                item.RuleFor(x => x.DetailsJson).MaximumLength(8000);
            });
        });
    }
}
