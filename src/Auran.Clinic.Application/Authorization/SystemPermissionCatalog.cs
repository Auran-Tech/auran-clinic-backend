using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Authorization;

public static class SystemPermissionCatalog
{
    public static readonly IReadOnlyCollection<PermissionDefinition> All = new PermissionDefinition[]
    {
        new(Permissions.Platform.Clinics.View, "Platform Clinics", PermissionScope.Platform, "View clinics.", "عرض العيادات."),
        new(Permissions.Platform.Clinics.Create, "Platform Clinics", PermissionScope.Platform, "Create and provision clinics.", "إنشاء وتجهيز العيادات."),
        new(Permissions.Platform.Clinics.Update, "Platform Clinics", PermissionScope.Platform, "Update clinic information.", "تعديل بيانات العيادة."),
        new(Permissions.Platform.Clinics.SetStatus, "Platform Clinics", PermissionScope.Platform, "Activate or suspend a clinic and all clinic access.", "تفعيل أو إيقاف العيادة وكل الحسابات التابعة لها."),
        new(Permissions.Platform.Clinics.ManageFeatures, "Platform Clinics", PermissionScope.Platform, "Manage clinic feature availability.", "إدارة الخصائص المتاحة للعيادة."),
        new(Permissions.Platform.AuditLogs.View, "Platform Audit", PermissionScope.Platform, "View platform audit history.", "عرض سجل تدقيق المنصة."),
        new(Permissions.Platform.Users.Manage, "Platform Users", PermissionScope.Platform, "Manage platform users.", "إدارة مستخدمي المنصة."),

        new(Permissions.Clinic.AuditLogs.View, "Administration", PermissionScope.Clinic, "View clinic audit history.", "عرض سجل التدقيق الخاص بالعيادة."),
        new(Permissions.Clinic.Patients.View, "Patients", PermissionScope.Clinic, "Search and view patient profiles.", "البحث عن المرضى وعرض ملفاتهم."),
        new(Permissions.Clinic.Patients.Create, "Patients", PermissionScope.Clinic, "Create a new patient record.", "إنشاء سجل مريض جديد."),
        new(Permissions.Clinic.Patients.Update, "Patients", PermissionScope.Clinic, "Edit patient basic information.", "تعديل البيانات الأساسية للمريض."),
        new(Permissions.Clinic.Users.View, "Administration", PermissionScope.Clinic, "View clinic users.", "عرض مستخدمي العيادة."),
        new(Permissions.Clinic.Users.Manage, "Administration", PermissionScope.Clinic, "Create and edit clinic users and role assignments.", "إنشاء وتعديل مستخدمي العيادة وتعيين الأدوار."),
        new(Permissions.Clinic.Users.ManageStatus, "Administration", PermissionScope.Clinic, "Enable or disable clinic user accounts.", "تفعيل أو تعطيل حسابات مستخدمي العيادة."),
        new(Permissions.Clinic.Roles.View, "Administration", PermissionScope.Clinic, "View roles and permission assignments.", "عرض الأدوار وتعيينات الصلاحيات."),
        new(Permissions.Clinic.Roles.Manage, "Administration", PermissionScope.Clinic, "Manage role permission assignments.", "إدارة تعيين الصلاحيات للأدوار."),
        new(Permissions.Clinic.Queue.View, "Clinic Workflow", PermissionScope.Clinic, "View the live clinic queue.", "عرض قائمة انتظار العيادة المباشرة."),
        new(Permissions.Clinic.Queue.Manage, "Clinic Workflow", PermissionScope.Clinic, "Move patients through allowed workflow transitions.", "نقل المرضى بين حالات مسار العمل المسموح بها."),
        new(Permissions.Clinic.Visits.View, "Visits", PermissionScope.Clinic, "View visits and sessions.", "عرض الزيارات والجلسات."),
        new(Permissions.Clinic.Visits.Create, "Visits", PermissionScope.Clinic, "Start a visit or doctor session.", "بدء زيارة أو جلسة طبيب."),
        new(Permissions.Clinic.Visits.Update, "Visits", PermissionScope.Clinic, "Edit visit documentation.", "تعديل توثيق الزيارة."),
        new(Permissions.Clinic.Clinical.View, "Clinical", PermissionScope.Clinic, "View patient clinical information.", "عرض البيانات السريرية للمريض."),
        new(Permissions.Clinic.Clinical.Manage, "Clinical", PermissionScope.Clinic, "Edit patient clinical information and measurements.", "تعديل البيانات والقياسات السريرية للمريض."),
        new(Permissions.Clinic.FollowUps.View, "Follow Ups", PermissionScope.Clinic, "View patient follow-up recommendations.", "عرض توصيات متابعة المرضى."),
        new(Permissions.Clinic.FollowUps.Manage, "Follow Ups", PermissionScope.Clinic, "Manage patient follow-up recommendations.", "إدارة توصيات متابعة المرضى."),
        new(Permissions.Clinic.Reports.View, "Reports", PermissionScope.Clinic, "View clinic reports and KPIs.", "عرض تقارير ومؤشرات أداء العيادة."),
        new(Permissions.Clinic.Reports.Export, "Reports", PermissionScope.Clinic, "Export permitted clinic reports.", "تصدير تقارير العيادة المسموح بها."),
        new(Permissions.Clinic.Settings.View, "Administration", PermissionScope.Clinic, "View clinic settings.", "عرض إعدادات العيادة."),
        new(Permissions.Clinic.Settings.Manage, "Administration", PermissionScope.Clinic, "Manage clinic identity, localization and settings.", "إدارة هوية العيادة واللغة والإعدادات."),
        new(Permissions.Clinic.Files.View, "Files", PermissionScope.Clinic, "View and download authorized clinic files.", "عرض وتنزيل ملفات العيادة المصرح بها."),
        new(Permissions.Clinic.Files.Upload, "Files", PermissionScope.Clinic, "Upload files to the clinic file registry.", "رفع الملفات إلى سجل ملفات العيادة.")
    };

    public static IReadOnlyCollection<PermissionDefinition> Platform =>
        All.Where(x => x.Scope == PermissionScope.Platform).ToArray();

    public static IReadOnlyCollection<PermissionDefinition> Clinic =>
        All.Where(x => x.Scope == PermissionScope.Clinic).ToArray();
}
