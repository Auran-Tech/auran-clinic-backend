using Auran.Clinic.Application.ClinicalOrders;

namespace Auran.Clinic.UnitTests.ClinicalOrders;

public sealed class ClinicalOrderValidatorTests
{
    [Fact]
    public void Save_requires_visit_id()
    {
        var validator = new SaveClinicalOrderRequestValidator();

        var result = validator.Validate(new SaveClinicalOrderRequest());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Save_rejects_empty_structured_item_name()
    {
        var validator = new SaveClinicalOrderRequestValidator();

        var result = validator.Validate(new SaveClinicalOrderRequest
        {
            VisitId = Guid.NewGuid(),
            Sections =
            [
                new SaveClinicalOrderSectionRequest
                {
                    SectionDefinitionId = Guid.NewGuid(),
                    Items =
                    [
                        new SaveClinicalOrderItemRequest
                        {
                            Name = string.Empty
                        }
                    ]
                }
            ]
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Save_rejects_overlong_text_value()
    {
        var validator = new SaveClinicalOrderRequestValidator();

        var result = validator.Validate(new SaveClinicalOrderRequest
        {
            VisitId = Guid.NewGuid(),
            Sections =
            [
                new SaveClinicalOrderSectionRequest
                {
                    SectionDefinitionId = Guid.NewGuid(),
                    TextValue = new string('A', 8001)
                }
            ]
        });

        Assert.False(result.IsValid);
    }
}
