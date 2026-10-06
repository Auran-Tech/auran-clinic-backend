using Auran.Clinic.Application.Visits;

namespace Auran.Clinic.UnitTests.Visits;

public sealed class VisitValidatorTests
{
    [Fact]
    public void Start_visit_requires_patient_and_doctor()
    {
        var validator = new StartVisitRequestValidator();

        var result = validator.Validate(new StartVisitRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(StartVisitRequest.PatientId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(StartVisitRequest.DoctorId));
    }

    [Fact]
    public void Start_visit_accepts_valid_identifiers()
    {
        var validator = new StartVisitRequestValidator();

        var result = validator.Validate(new StartVisitRequest
        {
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid()
        });

        Assert.True(result.IsValid);
    }
}
