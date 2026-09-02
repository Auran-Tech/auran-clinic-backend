namespace Auran.Clinic.Application.Users;

public sealed class CreateUserRequest
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? Phone { get; init; }
    public bool IsSuperUser { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
