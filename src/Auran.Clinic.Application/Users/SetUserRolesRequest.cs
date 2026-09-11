namespace Auran.Clinic.Application.Users;

public sealed class SetUserRolesRequest
{
    public Guid UserId { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
