namespace Auran.Clinic.Application.Clinics;

public sealed record ClinicSummaryResponse(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    string? TimeZoneId,
    string? PatientNumberPrefix,
    DateTime CreatedDate);
