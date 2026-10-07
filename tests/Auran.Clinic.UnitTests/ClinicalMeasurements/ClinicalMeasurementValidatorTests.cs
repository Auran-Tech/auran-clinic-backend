using Auran.Clinic.Application.ClinicalMeasurements;

namespace Auran.Clinic.UnitTests.ClinicalMeasurements;

public sealed class ClinicalMeasurementValidatorTests
{
    [Fact]
    public void Record_requires_visit_and_at_least_one_value()
    {
        var validator = new RecordClinicalMeasurementsRequestValidator();

        var result = validator.Validate(new RecordClinicalMeasurementsRequest());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Record_accepts_basic_typed_measurement()
    {
        var validator = new RecordClinicalMeasurementsRequestValidator();

        var result = validator.Validate(new RecordClinicalMeasurementsRequest
        {
            VisitId = Guid.NewGuid(),
            Values =
            [
                new RecordClinicalMeasurementValueRequest
                {
                    FieldId = Guid.NewGuid(),
                    NumberValue = 18.5m
                }
            ]
        });

        Assert.True(result.IsValid);
    }
}
