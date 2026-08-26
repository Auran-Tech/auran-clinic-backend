namespace Auran.Clinic.Application.Authorization;

public static class PermissionPolicy
{
    public const string Prefix = "Permission:";
    public static string For(string permission) => $"{Prefix}{permission}";
}
