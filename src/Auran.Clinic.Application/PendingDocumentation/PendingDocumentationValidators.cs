using FluentValidation;

namespace Auran.Clinic.Application.PendingDocumentation;

public sealed class CompletePendingDocumentationRequestValidator
    : AbstractValidator<CompletePendingDocumentationRequest>
{
    public CompletePendingDocumentationRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.ChiefComplaint).MaximumLength(4000);
        RuleFor(x => x.Examination).MaximumLength(8000);
        RuleFor(x => x.Diagnosis).MaximumLength(8000);
        RuleFor(x => x.Notes).MaximumLength(8000);
        RuleFor(x => x.TreatmentPlan).MaximumLength(8000);

        RuleFor(x => x)
            .Must(HasDocumentation)
            .WithMessage("At least one clinical documentation field is required.");
    }

    private static bool HasDocumentation(CompletePendingDocumentationRequest request) =>
        !string.IsNullOrWhiteSpace(request.ChiefComplaint) ||
        !string.IsNullOrWhiteSpace(request.Examination) ||
        !string.IsNullOrWhiteSpace(request.Diagnosis) ||
        !string.IsNullOrWhiteSpace(request.Notes) ||
        !string.IsNullOrWhiteSpace(request.TreatmentPlan);
}
