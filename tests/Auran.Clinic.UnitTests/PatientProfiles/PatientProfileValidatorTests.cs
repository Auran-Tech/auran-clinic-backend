using Auran.Clinic.Application.PatientProfiles;

namespace Auran.Clinic.UnitTests.PatientProfiles;

public sealed class PatientProfileValidatorTests
{
    [Fact]
    public void Save_requires_patient_id()
    {
        var validator = new SavePatientProfileRequestValidator();

        var result = validator.Validate(new SavePatientProfileRequest());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Save_rejects_overlong_text_values()
    {
        var validator = new SavePatientProfileRequestValidator();

        var result = validator.Validate(new SavePatientProfileRequest
        {
            PatientId = Guid.NewGuid(),
            Values =
            [
                new SavePatientProfileValueRequest
                {
                    FieldId = Guid.NewGuid(),
                    TextValue = new string('A', 8001)
                }
            ]
        });

        Assert.False(result.IsValid);
    }
}
