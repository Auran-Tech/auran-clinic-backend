namespace Auran.Clinic.Application.Reports;

public interface IReportService
{
    Task<OperationalReportResponse> GetOperationalAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);
}
