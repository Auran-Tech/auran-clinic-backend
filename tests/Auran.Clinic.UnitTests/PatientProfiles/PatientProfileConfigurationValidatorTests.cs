using Auran.Clinic.Application.PatientProfiles;

namespace Auran.Clinic.UnitTests.PatientProfiles;

public sealed class PatientProfileConfigurationValidatorTests
{
    [Fact]
    public void Create_field_rejects_unknown_field_type()
    {
        var validator = new CreatePatientProfileFieldRequestValidator();

        var result = validator.Validate(new CreatePatientProfileFieldRequest
        {
            SectionId = Guid.NewGuid(),
            Label = "Preferred language",
            FieldType = "Unknown",
            SortOrder = 0
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_field_accepts_supported_field_type()
    {
        var validator = new CreatePatientProfileFieldRequestValidator();

        var result = validator.Validate(new CreatePatientProfileFieldRequest
        {
            SectionId = Guid.NewGuid(),
            Label = "Preferred language",
            FieldType = "SingleSelect",
            SortOrder = 0
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_option_requires_field_label_and_value()
    {
        var validator = new CreatePatientProfileOptionRequestValidator();

        var result = validator.Validate(new CreatePatientProfileOptionRequest());

        Assert.False(result.IsValid);
    }
}
