namespace Auran.Clinic.Application.Clinics;

public sealed class CreateClinicRequest
{
    public required string Name { get; init; }
    public required string CodePrefix { get; init; }
    public string? TimeZoneId { get; init; }
    public string? PatientNumberPrefix { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public required InitialClinicAdminRequest Admin { get; init; }
}

public sealed class InitialClinicAdminRequest
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? Phone { get; init; }
}

public enum ClinicProvisioningFailure
{
    None = 0,
    Validation = 1,
    Conflict = 2
}

public sealed class ClinicProvisioningResult
{
    public bool Succeeded { get; init; }
    public ClinicProvisioningFailure Failure { get; init; }
    public string? Error { get; init; }
    public Guid? ClinicId { get; init; }
    public string? ClinicCode { get; init; }
    public Guid? AdminUserId { get; init; }
}

public interface IPlatformClinicProvisioningService
{
    Task<ClinicProvisioningResult> ProvisionAsync(
        CreateClinicRequest request,
        CancellationToken cancellationToken = default);
}
