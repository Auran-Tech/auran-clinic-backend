namespace Auran.Clinic.Application.Visits;

public interface IVisitService
{
    Task<VisitStartResult> StartAsync(
        StartVisitRequest request,
        CancellationToken cancellationToken = default);

    Task<VisitResponse?> GetActiveForPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientVisitHistoryResponse?> ListForPatientAsync(
        PatientVisitHistoryQuery query,
        CancellationToken cancellationToken = default);
}
