using Auran.Clinic.Application.ClinicalSessions;

namespace Auran.Clinic.UnitTests.ClinicalSessions;

public sealed class ClinicalSessionValidatorTests
{
    [Fact]
    public void Start_requires_visit_id()
    {
        var validator = new StartClinicalSessionRequestValidator();
        var result = validator.Validate(new StartClinicalSessionRequest());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Documentation_rejects_overlong_fields()
    {
        var validator = new SaveClinicalDocumentationRequestValidator();
        var result = validator.Validate(new SaveClinicalDocumentationRequest
        {
            VisitId = Guid.NewGuid(),
            Diagnosis = new string('A', 8001)
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void End_requires_visit_id()
    {
        var validator = new EndClinicalSessionRequestValidator();
        var result = validator.Validate(new EndClinicalSessionRequest());

        Assert.False(result.IsValid);
    }
}
