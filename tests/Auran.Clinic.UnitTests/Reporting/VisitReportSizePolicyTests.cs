using Auran.Clinic.Application.Reporting;

namespace Auran.Clinic.UnitTests.Reporting;

public sealed class VisitReportSizePolicyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9_999)]
    [InlineData(10_000)]
    public void Report_at_or_below_limit_is_accepted(int count)
    {
        VisitReportSizePolicy.EnsureWithinLimit(count);
    }

    [Theory]
    [InlineData(10_001)]
    [InlineData(25_000)]
    public void Report_exceeding_limit_is_rejected_before_export_generation(int count)
    {
        var exception = Assert.Throws<ReportResultTooLargeException>(
            () => VisitReportSizePolicy.EnsureWithinLimit(count));
        Assert.Equal(10_000, exception.MaximumRows);
        Assert.Contains("Narrow your filters", exception.Message);
    }
}
