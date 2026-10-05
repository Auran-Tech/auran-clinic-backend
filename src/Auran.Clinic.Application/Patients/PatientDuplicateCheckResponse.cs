namespace Auran.Clinic.Application.Patients;

public sealed record PatientDuplicateCandidate(
    Guid Id,
    string PatientNumber,
    string FullName,
    string Phone,
    DateOnly? DateOfBirth,
    IReadOnlyCollection<string> MatchReasons);

public sealed record PatientDuplicateCheckResponse(
    bool HasPotentialDuplicates,
    IReadOnlyCollection<PatientDuplicateCandidate> Candidates);
