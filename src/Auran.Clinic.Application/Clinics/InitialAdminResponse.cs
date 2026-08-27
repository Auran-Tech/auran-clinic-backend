namespace Auran.Clinic.Application.Clinics;

public sealed class InitialAdminResponse
{
    public Guid UserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string Role { get; set; }
}
