using Auran.Clinic.Application.ClinicalMeasurements;

namespace Auran.Clinic.UnitTests.ClinicalMeasurements;

public sealed class ClinicalFieldConfigurationValidatorTests
{
    [Fact]
    public void Create_field_rejects_unknown_type()
    {
        var validator = new CreateClinicalFieldRequestValidator();

        var result = validator.Validate(new CreateClinicalFieldRequest
        {
            Name = "Intraocular pressure",
            FieldType = "Unknown",
            SortOrder = 0
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_field_accepts_number_with_unit()
    {
        var validator = new CreateClinicalFieldRequestValidator();

        var result = validator.Validate(new CreateClinicalFieldRequest
        {
            Name = "Intraocular pressure",
            FieldType = "Number",
            Unit = "mmHg",
            SortOrder = 0
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_option_requires_field_label_and_value()
    {
        var validator = new CreateClinicalFieldOptionRequestValidator();

        var result = validator.Validate(new CreateClinicalFieldOptionRequest());

        Assert.False(result.IsValid);
    }
}
