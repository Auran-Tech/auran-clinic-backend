namespace Auran.Clinic.Application.Patients;

public sealed record PatientResponse(
    Guid Id,
    string PatientNumber,
    string FullName,
    string Phone,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Notes,
    DateTime CreatedDate,
    DateTime? UpdatedDate);
