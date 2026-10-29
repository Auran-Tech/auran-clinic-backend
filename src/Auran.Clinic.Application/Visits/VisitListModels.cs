using Auran.Clinic.Application.Models;
using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Visits;

public sealed class VisitListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public Guid? DoctorId { get; init; }
    public VisitStatus? Status { get; init; }
    public DocumentationStatus? DocumentationStatus { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string SortBy { get; init; } = "entryAtUtc";
    public bool Descending { get; init; } = true;
}

public sealed record VisitListItemResponse(
    Guid VisitId,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    string Status,
    string DocumentationStatus,
    DateTime EntryAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ExitAtUtc);

public sealed record VisitListResponse(
    PaginatedResponse<VisitListItemResponse> Visits,
    VisitListSummaryResponse Summary);

public sealed record VisitListSummaryResponse(
    int Active,
    int Completed,
    int PendingDocumentation);
