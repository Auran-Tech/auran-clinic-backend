namespace Auran.Clinic.Application.Authorization;

public static class SystemRoleCatalog
{
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string Doctor = "Doctor";
    public const string Nurse = "Nurse";

    private static readonly string[] AdminPermissions = SystemPermissionCatalog.All
        .Select(x => x.Code)
        .Where(x => x != Permissions.Clinics.Create)
        .ToArray();

    public static IReadOnlyList<SystemRoleDefinition> All { get; } = new List<SystemRoleDefinition>
    {
        new(Admin, "Admin", AdminPermissions),
        new(Receptionist, "Receptionist", new[]
        {
            Permissions.Clinics.View,
            Permissions.Clinics.SettingsView,
            Permissions.Patients.View,
            Permissions.Patients.Create,
            Permissions.Patients.Update,
            Permissions.Queue.View,
            Permissions.Queue.Manage,
            Permissions.Visits.View,
            Permissions.Visits.Create,
            Permissions.FollowUps.View
        }),
        new(Doctor, "Doctor", new[]
        {
            Permissions.Clinics.View,
            Permissions.Patients.View,
            Permissions.Queue.View,
            Permissions.Visits.View,
            Permissions.Visits.Create,
            Permissions.Visits.Update,
            Permissions.Clinical.View,
            Permissions.Clinical.Manage,
            Permissions.FollowUps.View,
            Permissions.FollowUps.Manage,
            Permissions.Reports.View
        }),
        new(Nurse, "Nurse", new[]
        {
            Permissions.Clinics.View,
            Permissions.Patients.View,
            Permissions.Queue.View,
            Permissions.Visits.View,
            Permissions.Clinical.View,
            Permissions.Clinical.Manage,
            Permissions.FollowUps.View
        })
    };
}
