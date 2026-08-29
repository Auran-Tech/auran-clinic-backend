using FluentValidation;

namespace Auran.Clinic.Application.Files;

public sealed class CreateFileUploadSessionRequestValidator : AbstractValidator<CreateFileUploadSessionRequest>
{
    private const long AbsoluteMaxFileSize = 100L * 1024 * 1024;

    public CreateFileUploadSessionRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Size)
            .GreaterThan(0)
            .LessThanOrEqualTo(AbsoluteMaxFileSize)
            .WithMessage("File size must be between 1 byte and 100 MB.");
    }
}
