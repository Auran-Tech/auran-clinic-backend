using Auran.Clinic.Application.Reporting;

namespace Auran.Clinic.UnitTests.Reporting;

public sealed class QueueReportQueryValidatorTests
{
    [Fact]
    public void Reversed_dates_are_rejected()
    {
        var result = new QueueReportQueryValidator().Validate(new QueueReportQuery
        {
            FromDate = new DateOnly(2026, 10, 17),
            ToDate = new DateOnly(2026, 10, 1)
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Doctor_and_status_filters_are_accepted()
    {
        var result = new QueueReportQueryValidator().Validate(new QueueReportQuery
        {
            DoctorId = Guid.NewGuid(), WorkflowStatusId = Guid.NewGuid(),
            FromDate = new DateOnly(2026, 10, 1), ToDate = new DateOnly(2026, 10, 17)
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Date_limit_rejects_overflow()
    {
        var result = new QueueReportQueryValidator().Validate(new QueueReportQuery { ToDate = DateOnly.MaxValue });
        Assert.False(result.IsValid);
    }
}
