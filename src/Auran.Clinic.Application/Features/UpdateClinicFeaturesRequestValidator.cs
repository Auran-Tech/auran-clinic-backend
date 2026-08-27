using FluentValidation;

namespace Auran.Clinic.Application.Features;

public sealed class UpdateClinicFeaturesRequestValidator : AbstractValidator<UpdateClinicFeaturesRequest>
{
    public UpdateClinicFeaturesRequestValidator()
    {
        RuleFor(x => x.Features)
            .NotEmpty()
            .Must(items => items.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == items.Count)
            .WithMessage("Feature codes must be unique.");

        RuleForEach(x => x.Features).ChildRules(item =>
        {
            item.RuleFor(x => x.Code)
                .NotEmpty()
                .MaximumLength(100)
                .Must(code => SystemFeatureCatalog.All.Any(x =>
                    x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
                .WithMessage("Unknown feature code.");

            item.RuleFor(x => x.ConfigurationJson)
                .MaximumLength(8000);
        });
    }
}
