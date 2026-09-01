namespace Auran.Clinic.Application.Authorization;

public static class SystemPermissionCatalog
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(Permissions.Audit.View, "Audit", "View audit logs", "عرض سجل التدقيق"),

        new(Permissions.Patients.View, "Patient", "View patient information", "عرض بيانات المرضى", "Patients.View"),
        new(Permissions.Patients.Create, "Patient", "Create patients", "إنشاء مريض", "Patients.Create"),
        new(Permissions.Patients.Update, "Patient", "Edit basic patient information", "تعديل البيانات الأساسية للمريض", "Patients.Update"),

        new(Permissions.Users.View, "Users", "View users", "عرض المستخدمين", "Users.View"),
        new(Permissions.Users.Manage, "Users", "Manage users", "إدارة المستخدمين", "Users.Manage"),
        new(Permissions.Users.ManageStatus, "Users", "Change user account status", "تغيير حالة حسابات المستخدمين"),

        new(Permissions.Roles.View, "RBAC", "View roles and permissions", "عرض الأدوار والصلاحيات", "Roles.View"),
        new(Permissions.Roles.Manage, "RBAC", "Manage roles and permissions", "إدارة الأدوار والصلاحيات", "Roles.Manage"),

        new(Permissions.Queue.View, "Queue", "View the clinic queue", "عرض قائمة الانتظار"),
        new(Permissions.Queue.Move, "Queue", "Move or change queue entries", "تغيير ترتيب أو حالة قائمة الانتظار"),

        new(Permissions.Visits.View, "Visit", "View visits", "عرض الزيارات"),
        new(Permissions.Visits.Start, "Visit", "Start visits", "بدء زيارة"),
        new(Permissions.Visits.Edit, "Visit", "Edit visits", "تعديل الزيارة"),

        new(Permissions.MedicalProfile.View, "MedicalProfile", "View medical profiles", "عرض الملف الطبي"),
        new(Permissions.MedicalProfile.Edit, "MedicalProfile", "Edit medical profiles", "تعديل الملف الطبي"),

        new(Permissions.FollowUps.View, "FollowUp", "View follow-ups", "عرض المتابعات"),
        new(Permissions.FollowUps.Manage, "FollowUp", "Manage follow-ups", "إدارة المتابعات"),

        new(Permissions.Reports.View, "Reports", "View reports", "عرض التقارير"),
        new(Permissions.Reports.Export, "Reports", "Export reports", "تصدير التقارير"),

        new(Permissions.Settings.View, "Settings", "View clinic settings", "عرض الإعدادات", "Settings.View"),
        new(Permissions.Settings.Manage, "Settings", "Manage clinic settings", "إدارة الإعدادات", "Settings.Manage"),

        new(Permissions.Files.View, "Files", "View files", "عرض الملفات"),
        new(Permissions.Files.Upload, "Files", "Upload files", "رفع الملفات"),

        new(Permissions.Attendance.CreateShift, "Attendance", "Create work schedules", "انشاء مواعيد العمل")
    ];
}
