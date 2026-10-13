using FluentValidation;

namespace Auran.Clinic.Application.ClinicalOrderAttachments;

public sealed class LinkClinicalOrderAttachmentRequestValidator
    : AbstractValidator<LinkClinicalOrderAttachmentRequest>
{
    public LinkClinicalOrderAttachmentRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.FileId).NotEmpty();
        RuleFor(x => x.SectionDefinitionId).NotEmpty();
    }
}

public sealed class DeleteClinicalOrderAttachmentRequestValidator
    : AbstractValidator<DeleteClinicalOrderAttachmentRequest>
{
    public DeleteClinicalOrderAttachmentRequestValidator() =>
        RuleFor(x => x.LinkId).NotEmpty();
}
