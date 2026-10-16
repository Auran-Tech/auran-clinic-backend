namespace Auran.Clinic.Application.Reporting;

public interface IReportingService
{
    Task<PatientReportResponse> GetPatientReportAsync(
        PatientReportQuery query,
        CancellationToken cancellationToken = default);

    Task<ReportExportResponse> ExportPatientReportCsvAsync(
        PatientReportQuery query,
        CancellationToken cancellationToken = default);

    Task<DashboardSummaryResponse> GetDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<VisitReportResponse> GetVisitReportAsync(
        VisitReportQuery query,
        CancellationToken cancellationToken = default);

    Task<ReportExportResponse> ExportVisitReportCsvAsync(
        VisitReportQuery query,
        CancellationToken cancellationToken = default);
}
