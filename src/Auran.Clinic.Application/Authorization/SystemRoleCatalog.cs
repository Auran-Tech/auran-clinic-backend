namespace Auran.Clinic.Application.Authorization;

public static class SystemRoleCatalog
{
    public const string Admin = "ADMIN";
    public const string Receptionist = "RECEPTIONIST";
    public const string Doctor = "DOCTOR";
    public const string Nurse = "NURSE";

    private static readonly string[] AllClinicPermissions =
        SystemPermissionCatalog.Clinic.Select(x => x.Code).ToArray();

    public static readonly IReadOnlyCollection<SystemRoleDefinition> All = new[]
    {
        new SystemRoleDefinition(Admin, "Admin", AllClinicPermissions),
        new SystemRoleDefinition(
            Receptionist,
            "Receptionist",
            new[]
            {
                Permissions.Clinic.Patients.View,
                Permissions.Clinic.Patients.Create,
                Permissions.Clinic.Patients.Update,
                Permissions.Clinic.Queue.View,
                Permissions.Clinic.Queue.Manage,
                Permissions.Clinic.Visits.View,
                Permissions.Clinic.Visits.Create,
                Permissions.Clinic.FollowUps.View,
                Permissions.Clinic.Files.View,
                Permissions.Clinic.Files.Upload
            }),
        new SystemRoleDefinition(
            Doctor,
            "Doctor",
            new[]
            {
                Permissions.Clinic.Patients.View,
                Permissions.Clinic.Visits.View,
                Permissions.Clinic.Visits.Create,
                Permissions.Clinic.Visits.Update,
                Permissions.Clinic.Clinical.View,
                Permissions.Clinic.Clinical.Manage,
                Permissions.Clinic.FollowUps.View,
                Permissions.Clinic.FollowUps.Manage,
                Permissions.Clinic.Reports.View,
                Permissions.Clinic.Files.View,
                Permissions.Clinic.Files.Upload
            }),
        new SystemRoleDefinition(
            Nurse,
            "Nurse",
            new[]
            {
                Permissions.Clinic.Patients.View,
                Permissions.Clinic.Queue.View,
                Permissions.Clinic.Queue.Manage,
                Permissions.Clinic.Visits.View,
                Permissions.Clinic.Clinical.View,
                Permissions.Clinic.Clinical.Manage,
                Permissions.Clinic.Files.View,
                Permissions.Clinic.Files.Upload
            })
    };
}
