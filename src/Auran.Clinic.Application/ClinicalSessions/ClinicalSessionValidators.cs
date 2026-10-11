using FluentValidation;

namespace Auran.Clinic.Application.ClinicalSessions;

public sealed class StartClinicalSessionRequestValidator : AbstractValidator<StartClinicalSessionRequest>
{
    public StartClinicalSessionRequestValidator() =>
        RuleFor(x => x.VisitId).NotEmpty();
}

public sealed class EndClinicalSessionRequestValidator : AbstractValidator<EndClinicalSessionRequest>
{
    public EndClinicalSessionRequestValidator() =>
        RuleFor(x => x.VisitId).NotEmpty();
}

public sealed class SaveClinicalDocumentationRequestValidator : AbstractValidator<SaveClinicalDocumentationRequest>
{
    public SaveClinicalDocumentationRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.ChiefComplaint).MaximumLength(4000);
        RuleFor(x => x.Examination).MaximumLength(8000);
        RuleFor(x => x.Diagnosis).MaximumLength(8000);
        RuleFor(x => x.Notes).MaximumLength(8000);
        RuleFor(x => x.TreatmentPlan).MaximumLength(8000);
    }
}
