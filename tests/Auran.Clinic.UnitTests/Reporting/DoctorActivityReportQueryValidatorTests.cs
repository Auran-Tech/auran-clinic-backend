using Auran.Clinic.Application.Reporting;

namespace Auran.Clinic.UnitTests.Reporting;

public sealed class DoctorActivityReportQueryValidatorTests
{
    [Fact]
    public void Reversed_dates_are_rejected()
    {
        var result = new DoctorActivityReportQueryValidator().Validate(new DoctorActivityReportQuery
        {
            FromDate = new DateOnly(2026, 10, 17), ToDate = new DateOnly(2026, 10, 1)
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Valid_filters_are_accepted()
    {
        var result = new DoctorActivityReportQueryValidator().Validate(new DoctorActivityReportQuery
        {
            FromDate = new DateOnly(2026, 10, 1), ToDate = new DateOnly(2026, 10, 17), DoctorId = Guid.NewGuid()
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Invalid_date_or_empty_doctor_are_rejected()
    {
        var validator = new DoctorActivityReportQueryValidator();
        Assert.False(validator.Validate(new DoctorActivityReportQuery { ToDate = DateOnly.MaxValue }).IsValid);
        Assert.False(validator.Validate(new DoctorActivityReportQuery { DoctorId = Guid.Empty }).IsValid);
    }
}
