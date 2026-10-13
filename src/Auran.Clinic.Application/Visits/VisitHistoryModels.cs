using Auran.Clinic.Application.Models;

namespace Auran.Clinic.Application.Visits;

public sealed class PatientVisitHistoryQuery
{
    public Guid PatientId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public sealed record PatientVisitHistoryItemResponse(
    Guid VisitId,
    Guid DoctorId,
    string DoctorName,
    string Status,
    string DocumentationStatus,
    DateTime EntryAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ExitAtUtc,
    string? ChiefComplaint,
    string? Diagnosis,
    int SessionCount);

public sealed record PatientVisitHistoryResponse(
    Guid PatientId,
    PaginatedResponse<PatientVisitHistoryItemResponse> Visits);
