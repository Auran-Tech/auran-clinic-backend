using Auran.Clinic.Application.Reporting;

namespace Auran.Clinic.UnitTests.Reporting;

public sealed class ReportingValidatorTests
{
    [Fact]
    public void Visit_report_rejects_reversed_date_range()
    {
        var validator = new VisitReportQueryValidator();

        var result = validator.Validate(new VisitReportQuery
        {
            FromDate = new DateOnly(2026, 10, 10),
            ToDate = new DateOnly(2026, 10, 1)
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Visit_report_accepts_known_status_filters()
    {
        var validator = new VisitReportQueryValidator();

        var result = validator.Validate(new VisitReportQuery
        {
            VisitStatus = "Completed",
            DocumentationStatus = "Pending"
        });

        Assert.True(result.IsValid);
    }
}
