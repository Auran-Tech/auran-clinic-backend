namespace Auran.Clinic.Application.Reporting;

/// <summary>
/// A hard safety boundary for the incremental visit report until
/// a reviewed paginated/streamed reporting API replaces it.
/// </summary>
public static class VisitReportSizePolicy
{
    public const int MaximumRows = 10_000;

    public static void EnsureWithinLimit(int retrievedRows)
    {
        if (retrievedRows > MaximumRows)
            throw new ReportResultTooLargeException(MaximumRows);
    }
}

public sealed class ReportResultTooLargeException(int maximumRows)
    : Exception($"Report exceeds the {maximumRows} row limit. Narrow your filters.")
{
    public int MaximumRows { get; } = maximumRows;
}
