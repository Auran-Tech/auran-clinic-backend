using Auran.Clinic.Application.Workflow;

namespace Auran.Clinic.UnitTests.Workflow;

public sealed class WorkflowConfigurationValidatorTests
{
    [Fact]
    public void Status_requires_safe_code_and_hex_color()
    {
        var validator = new CreateWorkflowStatusRequestValidator();

        var result = validator.Validate(new CreateWorkflowStatusRequest
        {
            Code = "Waiting Room",
            Name = "Waiting",
            Color = "red",
            SortOrder = 0
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Status_accepts_valid_configuration()
    {
        var validator = new CreateWorkflowStatusRequestValidator();

        var result = validator.Validate(new CreateWorkflowStatusRequest
        {
            Code = "WAITING",
            Name = "Waiting",
            Color = "#64748b",
            SortOrder = 10
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Transition_rejects_self_loop()
    {
        var id = Guid.NewGuid();
        var validator = new ReplaceWorkflowTransitionsRequestValidator();

        var result = validator.Validate(new ReplaceWorkflowTransitionsRequest
        {
            Transitions =
            [
                new WorkflowTransitionInput
                {
                    FromStatusId = id,
                    ToStatusId = id
                }
            ]
        });

        Assert.False(result.IsValid);
    }
}
