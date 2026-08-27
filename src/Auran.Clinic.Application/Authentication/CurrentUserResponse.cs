namespace Auran.Clinic.Application.Authentication;

public sealed class CurrentUserResponse
{
    public Guid UserId { get; init; }
    public Guid ClinicId { get; init; }
    public required string FullName { get; init; }
    public string? Email { get; init; }
    public bool IsSuperUser { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
}
