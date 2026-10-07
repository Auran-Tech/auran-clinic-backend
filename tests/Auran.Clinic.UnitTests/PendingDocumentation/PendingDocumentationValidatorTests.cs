using Auran.Clinic.Application.PendingDocumentation;

namespace Auran.Clinic.UnitTests.PendingDocumentation;

public sealed class PendingDocumentationValidatorTests
{
    [Fact]
    public void Complete_requires_visit_and_content()
    {
        var validator = new CompletePendingDocumentationRequestValidator();

        var result = validator.Validate(new CompletePendingDocumentationRequest());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Complete_accepts_at_least_one_documentation_field()
    {
        var validator = new CompletePendingDocumentationRequestValidator();

        var result = validator.Validate(new CompletePendingDocumentationRequest
        {
            VisitId = Guid.NewGuid(),
            Notes = "Completed after the patient left."
        });

        Assert.True(result.IsValid);
    }
}
