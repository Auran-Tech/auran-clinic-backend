using FluentValidation;

namespace Auran.Clinic.Application.Settings;

public sealed record WorkflowStatusSettingsResponse(
    string Code,
    string Name,
    string Color,
    int SortOrder,
    bool IsFinal);

public sealed record WorkflowTransitionSettingsResponse(
    string FromCode,
    string ToCode);

public sealed record WorkflowSettingsResponse(
    IReadOnlyCollection<WorkflowStatusSettingsResponse> Statuses,
    IReadOnlyCollection<WorkflowTransitionSettingsResponse> Transitions);

public sealed class WorkflowStatusSettingsRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Color { get; init; }
    public int SortOrder { get; init; }
    public bool IsFinal { get; init; }
}

public sealed class WorkflowTransitionSettingsRequest
{
    public required string FromCode { get; init; }
    public required string ToCode { get; init; }
}

public sealed class SaveWorkflowSettingsRequest
{
    public IReadOnlyCollection<WorkflowStatusSettingsRequest> Statuses { get; init; } = [];
    public IReadOnlyCollection<WorkflowTransitionSettingsRequest> Transitions { get; init; } = [];
}

public sealed class SaveWorkflowSettingsRequestValidator : AbstractValidator<SaveWorkflowSettingsRequest>
{
    public SaveWorkflowSettingsRequestValidator()
    {
        RuleFor(x => x.Statuses).NotEmpty();
        RuleForEach(x => x.Statuses).ChildRules(status =>
        {
            status.RuleFor(x => x.Code).NotEmpty().MaximumLength(64).Matches("^[A-Za-z0-9_-]+$");
            status.RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
            status.RuleFor(x => x.Color).NotEmpty().MaximumLength(32);
        });

        RuleFor(x => x.Statuses)
            .Must(statuses => statuses.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == statuses.Count)
            .WithMessage("Workflow status codes must be unique.");

        RuleFor(x => x.Statuses)
            .Must(statuses => statuses.Count(x => x.IsFinal) >= 1)
            .WithMessage("At least one final workflow status is required.");
    }
}
