using System.Text.RegularExpressions;
using FluentValidation;

namespace Auran.Clinic.Application.Workflow;

public sealed class CreateWorkflowStatusRequestValidator
    : AbstractValidator<CreateWorkflowStatusRequest>
{
    private static readonly Regex CodePattern = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public CreateWorkflowStatusRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(32)
            .Must(code => CodePattern.IsMatch(code))
            .WithMessage("Code may contain only letters, digits, and underscore.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color)
            .NotEmpty()
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("Color must be a 6-digit hex value.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateWorkflowStatusRequestValidator
    : AbstractValidator<UpdateWorkflowStatusRequest>
{
    public UpdateWorkflowStatusRequestValidator()
    {
        Include(new CreateWorkflowStatusRequestValidator());
        RuleFor(x => x.StatusId).NotEmpty();
    }
}

public sealed class DeleteWorkflowStatusRequestValidator
    : AbstractValidator<DeleteWorkflowStatusRequest>
{
    public DeleteWorkflowStatusRequestValidator() =>
        RuleFor(x => x.StatusId).NotEmpty();
}

public sealed class ReplaceWorkflowTransitionsRequestValidator
    : AbstractValidator<ReplaceWorkflowTransitionsRequest>
{
    public ReplaceWorkflowTransitionsRequestValidator()
    {
        RuleForEach(x => x.Transitions).ChildRules(transition =>
        {
            transition.RuleFor(x => x.FromStatusId).NotEmpty();
            transition.RuleFor(x => x.ToStatusId).NotEmpty();
            transition.RuleFor(x => x)
                .Must(x => x.FromStatusId != x.ToStatusId)
                .WithMessage("A workflow transition cannot target the same status.");
        });
    }
}
