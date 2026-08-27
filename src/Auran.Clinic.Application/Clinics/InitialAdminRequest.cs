namespace Auran.Clinic.Application.Clinics;

public sealed class InitialAdminRequest
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string Password { get; set; }
}
