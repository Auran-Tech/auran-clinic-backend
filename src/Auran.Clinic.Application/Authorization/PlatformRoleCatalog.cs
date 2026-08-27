namespace Auran.Clinic.Application.Authorization;

public static class PlatformRoleCatalog
{
    public const string PlatformAdmin = "PLATFORM_ADMIN";

    public static readonly IReadOnlyCollection<SystemRoleDefinition> All = new[]
    {
        new SystemRoleDefinition(
            PlatformAdmin,
            "Platform Admin",
            SystemPermissionCatalog.Platform.Select(x => x.Code).ToArray())
    };
}
