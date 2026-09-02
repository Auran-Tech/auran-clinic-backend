namespace Auran.Clinic.Application.Users;

public sealed class SetUserRolesRequest
{
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
