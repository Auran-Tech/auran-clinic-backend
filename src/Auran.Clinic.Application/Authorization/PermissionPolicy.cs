namespace Auran.Clinic.Application.Authorization;

public static class PermissionPolicy
{
    public const string ClinicPrefix = "ClinicPermission:";
    public const string PlatformPrefix = "PlatformPermission:";

    public static string ForClinic(string permission) => $"{ClinicPrefix}{permission}";
    public static string ForPlatform(string permission) => $"{PlatformPrefix}{permission}";
}
