using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Authorization;

public static class SystemPermissionCatalog
{
    public static readonly IReadOnlyCollection<PermissionDefinition> All = new PermissionDefinition[]
    {
        new(Permissions.Platform.Clinics.View, "View clinics", "Platform Clinics", PermissionScope.Platform),
        new(Permissions.Platform.Clinics.Create, "Create clinics", "Platform Clinics", PermissionScope.Platform),
        new(Permissions.Platform.Clinics.Update, "Update clinics", "Platform Clinics", PermissionScope.Platform),
        new(Permissions.Platform.Clinics.SetStatus, "Activate or suspend clinics", "Platform Clinics", PermissionScope.Platform),
        new(Permissions.Platform.Clinics.ManageFeatures, "Manage clinic features", "Platform Clinics", PermissionScope.Platform),
        new(Permissions.Platform.AuditLogs.View, "View platform audit logs", "Platform Audit", PermissionScope.Platform),
        new(Permissions.Platform.Users.Manage, "Manage platform users", "Platform Users", PermissionScope.Platform),

        new(Permissions.Clinic.AuditLogs.View, "View clinic audit logs", "Audit", PermissionScope.Clinic),
        new(Permissions.Clinic.Patients.View, "View patients", "Patients", PermissionScope.Clinic),
        new(Permissions.Clinic.Patients.Create, "Create patients", "Patients", PermissionScope.Clinic),
        new(Permissions.Clinic.Patients.Update, "Update patients", "Patients", PermissionScope.Clinic),
        new(Permissions.Clinic.Users.View, "View users", "Users", PermissionScope.Clinic),
        new(Permissions.Clinic.Users.Manage, "Manage users", "Users", PermissionScope.Clinic),
        new(Permissions.Clinic.Roles.View, "View roles", "Roles", PermissionScope.Clinic),
        new(Permissions.Clinic.Roles.Manage, "Manage roles", "Roles", PermissionScope.Clinic),
        new(Permissions.Clinic.Queue.View, "View queue", "Queue", PermissionScope.Clinic),
        new(Permissions.Clinic.Queue.Manage, "Manage queue", "Queue", PermissionScope.Clinic),
        new(Permissions.Clinic.Visits.View, "View visits", "Visits", PermissionScope.Clinic),
        new(Permissions.Clinic.Visits.Create, "Create visits", "Visits", PermissionScope.Clinic),
        new(Permissions.Clinic.Visits.Update, "Update visits", "Visits", PermissionScope.Clinic),
        new(Permissions.Clinic.Clinical.View, "View clinical data", "Clinical", PermissionScope.Clinic),
        new(Permissions.Clinic.Clinical.Manage, "Manage clinical data", "Clinical", PermissionScope.Clinic),
        new(Permissions.Clinic.FollowUps.View, "View follow-ups", "Follow Ups", PermissionScope.Clinic),
        new(Permissions.Clinic.FollowUps.Manage, "Manage follow-ups", "Follow Ups", PermissionScope.Clinic),
        new(Permissions.Clinic.Reports.View, "View reports", "Reports", PermissionScope.Clinic),
        new(Permissions.Clinic.Reports.Export, "Export reports", "Reports", PermissionScope.Clinic),
        new(Permissions.Clinic.Settings.View, "View clinic settings", "Settings", PermissionScope.Clinic),
        new(Permissions.Clinic.Settings.Manage, "Manage clinic settings", "Settings", PermissionScope.Clinic)
    };

    public static IReadOnlyCollection<PermissionDefinition> Platform =>
        All.Where(x => x.Scope == PermissionScope.Platform).ToArray();

    public static IReadOnlyCollection<PermissionDefinition> Clinic =>
        All.Where(x => x.Scope == PermissionScope.Clinic).ToArray();
}
