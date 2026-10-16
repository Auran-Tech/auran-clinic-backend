using Auran.Clinic.Application.Reporting;

namespace Auran.Clinic.UnitTests.Reporting;

public sealed class PatientReportQueryValidatorTests
{
    [Fact]
    public void Reversed_clinic_local_date_range_is_rejected()
    {
        var validator = new PatientReportQueryValidator();
        var result = validator.Validate(new PatientReportQuery
        {
            FromDate = new DateOnly(2026, 10, 16),
            ToDate = new DateOnly(2026, 10, 1)
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Valid_gender_and_date_filters_are_accepted()
    {
        var validator = new PatientReportQueryValidator();
        var result = validator.Validate(new PatientReportQuery
        {
            FromDate = new DateOnly(2026, 10, 1),
            ToDate = new DateOnly(2026, 10, 16),
            Gender = "Female"
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Extremely_long_gender_filter_is_rejected()
    {
        var validator = new PatientReportQueryValidator();
        var result = validator.Validate(new PatientReportQuery { Gender = new string('a', 33) });
        Assert.False(result.IsValid);
    }
}
