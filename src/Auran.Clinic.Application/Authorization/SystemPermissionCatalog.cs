namespace Auran.Clinic.Application.Authorization;

public static class SystemPermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } = new List<PermissionDefinition>
    {
        new(Permissions.Clinics.View, "View clinic", "Clinics"),
        new(Permissions.Clinics.Create, "Create clinic", "Clinics"),
        new(Permissions.Clinics.Update, "Update clinic", "Clinics"),
        new(Permissions.Clinics.Activate, "Activate or deactivate clinic", "Clinics"),
        new(Permissions.Clinics.SettingsView, "View clinic settings", "Clinics"),
        new(Permissions.Clinics.SettingsUpdate, "Update clinic settings", "Clinics"),
        new(Permissions.AuditLogs.View, "View audit logs", "Audit"),
        new(Permissions.Users.View, "View users", "Users"),
        new(Permissions.Users.Manage, "Manage users", "Users"),
        new(Permissions.Roles.View, "View roles", "Roles"),
        new(Permissions.Roles.Manage, "Manage roles", "Roles"),
        new(Permissions.Patients.View, "View patients", "Patients"),
        new(Permissions.Patients.Create, "Create patients", "Patients"),
        new(Permissions.Patients.Update, "Update patients", "Patients"),
        new(Permissions.Queue.View, "View live queue", "Queue"),
        new(Permissions.Queue.Manage, "Manage live queue", "Queue"),
        new(Permissions.Visits.View, "View visits", "Visits"),
        new(Permissions.Visits.Create, "Create visits", "Visits"),
        new(Permissions.Visits.Update, "Update visits", "Visits"),
        new(Permissions.Clinical.View, "View clinical data", "Clinical"),
        new(Permissions.Clinical.Manage, "Manage clinical data", "Clinical"),
        new(Permissions.FollowUps.View, "View follow-ups", "FollowUps"),
        new(Permissions.FollowUps.Manage, "Manage follow-ups", "FollowUps"),
        new(Permissions.Reports.View, "View reports", "Reports"),
        new(Permissions.Reports.Export, "Export reports", "Reports"),
        new(Permissions.Settings.View, "View system settings", "Settings"),
        new(Permissions.Settings.Manage, "Manage system settings", "Settings")
    };
}
