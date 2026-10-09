using Auran.Clinic.Application.ClinicalOrders;

namespace Auran.Clinic.UnitTests.ClinicalOrders;

public sealed class ClinicalOrderSectionConfigurationValidatorTests
{
    [Fact]
    public void Create_rejects_unknown_section_type()
    {
        var validator = new CreateClinicalOrderSectionDefinitionRequestValidator();

        var result = validator.Validate(new CreateClinicalOrderSectionDefinitionRequest
        {
            Name = "Imaging",
            SectionType = "Unknown",
            SortOrder = 0
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_accepts_supported_section_type()
    {
        var validator = new CreateClinicalOrderSectionDefinitionRequestValidator();

        var result = validator.Validate(new CreateClinicalOrderSectionDefinitionRequest
        {
            Name = "Imaging",
            SectionType = "Image",
            SortOrder = 0
        });

        Assert.True(result.IsValid);
    }
}
