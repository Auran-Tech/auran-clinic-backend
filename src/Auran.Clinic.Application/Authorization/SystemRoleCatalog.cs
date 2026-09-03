namespace Auran.Clinic.Application.Authorization;

public static class SystemRoleCatalog
{
    public const string Admin = "ADMIN";
    public const string Receptionist = "RECEPTIONIST";
    public const string Doctor = "DOCTOR";
    public const string Nurse = "NURSE";

    private static readonly string[] AllPermissions =
        SystemPermissionCatalog.All.Select(definition => definition.Key).ToArray();

    public static IReadOnlyCollection<SystemRoleDefinition> All { get; } =
    [
        new(Admin, "Admin", AllPermissions),
        new(
            Receptionist,
            "Receptionist",
            [
                Permissions.Patients.View,
                Permissions.Patients.Create,
                Permissions.Patients.Update,
                Permissions.Queue.View,
                Permissions.Queue.Move,
                Permissions.Visits.View,
                Permissions.Visits.Start,
                Permissions.FollowUps.View,
                Permissions.FollowUps.Manage,
                Permissions.Files.View,
                Permissions.Files.Upload
            ]),
        new(
            Doctor,
            "Doctor",
            [
                Permissions.Patients.View,
                Permissions.Visits.View,
                Permissions.Visits.Start,
                Permissions.Visits.Edit,
                Permissions.MedicalProfile.View,
                Permissions.MedicalProfile.Edit,
                Permissions.FollowUps.View,
                Permissions.FollowUps.Manage,
                Permissions.Reports.View,
                Permissions.Files.View,
                Permissions.Files.Upload
            ]),
        new(
            Nurse,
            "Nurse",
            [
                Permissions.Patients.View,
                Permissions.Queue.View,
                Permissions.Queue.Move,
                Permissions.Visits.View,
                Permissions.MedicalProfile.View,
                Permissions.MedicalProfile.Edit,
                Permissions.Files.View,
                Permissions.Files.Upload
            ])
    ];
}
