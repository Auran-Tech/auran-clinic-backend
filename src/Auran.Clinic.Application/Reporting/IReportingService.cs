namespace Auran.Clinic.Application.Reporting;

public interface IReportingService
{
    Task<DashboardSummaryResponse> GetDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<VisitReportResponse> GetVisitReportAsync(
        VisitReportQuery query,
        CancellationToken cancellationToken = default);

    Task<ReportExportResponse> ExportVisitReportCsvAsync(
        VisitReportQuery query,
        CancellationToken cancellationToken = default);
}
