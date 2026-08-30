namespace Auran.Clinic.Application.Authorization;

public static class Permissions
{
    public const string DashboardView = "Dashboard_View";
    public const string PatientView = "Patient_View";
    public const string PatientCreate = "Patient_Create";
    public const string PatientEditBasic = "Patient_Edit_Basic";
    public const string MedicalProfileView = "MedicalProfile_View";
    public const string MedicalProfileEdit = "MedicalProfile_Edit";
    public const string MeasurementCreate = "Measurement_Create";
    public const string QueueView = "Queue_View";
    public const string QueueCheckIn = "Queue_Check_In";
    public const string QueueMove = "Queue_Move";
    public const string QueueExit = "Queue_Exit";
    public const string VisitView = "Visit_View";
    public const string VisitStart = "Visit_Start";
    public const string VisitEdit = "Visit_Edit";
    public const string VisitSession = "Visit_Session";
    public const string PrescriptionCreate = "Prescription_Create";
    public const string DocumentationComplete = "Documentation_Complete";
    public const string FollowUpView = "FollowUp_View";
    public const string ReportsView = "Reports_View";
    public const string UsersManage = "Users_Manage";
    public const string UsersManageStatus = "Users_Manage_Status";
    public const string RbacView = "RBAC_View";
    public const string ConfigManage = "Config_Manage";
    public const string AuditView = "Audit_View";
    public const string SettingsManage = "Settings_Manage";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(DashboardView, "Dashboard", "Open the main clinic dashboard.", "فتح لوحة التحكم الرئيسية للعيادة."),
        new(PatientView, "Patients", "Search and open patient profiles.", "البحث عن المرضى وفتح ملفاتهم."),
        new(PatientCreate, "Patients", "Create a new patient record.", "إنشاء سجل مريض جديد."),
        new(PatientEditBasic, "Patients", "Edit patient basic information such as name, phone, gender and date of birth.", "تعديل البيانات الأساسية للمريض مثل الاسم والهاتف والنوع وتاريخ الميلاد."),
        new(MedicalProfileView, "Clinical", "View patient medical history, conditions, allergies and medications.", "عرض التاريخ الطبي والحالات والحساسيات والأدوية الخاصة بالمريض."),
        new(MedicalProfileEdit, "Clinical", "Update patient medical history, conditions, allergies and medications.", "تحديث التاريخ الطبي والحالات والحساسيات والأدوية الخاصة بالمريض."),
        new(MeasurementCreate, "Clinical", "Record configured clinical measurements for a patient.", "تسجيل القياسات الطبية المهيأة للمريض."),
        new(QueueView, "ClinicWorkflow", "View the live clinic workflow queue.", "عرض قائمة انتظار ومسار العمل المباشر للعيادة."),
        new(QueueCheckIn, "ClinicWorkflow", "Check a patient into the clinic workflow.", "إدخال المريض إلى مسار العمل داخل العيادة."),
        new(QueueMove, "ClinicWorkflow", "Move a patient through an allowed workflow transition.", "نقل المريض بين حالات مسار العمل المسموح بها."),
        new(QueueExit, "ClinicWorkflow", "Mark a patient as exited from the clinic workflow.", "تسجيل خروج المريض من مسار العمل بالعيادة."),
        new(VisitView, "Visits", "View patient visits and doctor sessions.", "عرض زيارات المرضى وجلسات الأطباء."),
        new(VisitStart, "Visits", "Start a doctor session inside a visit.", "بدء جلسة طبيب داخل الزيارة."),
        new(VisitEdit, "Visits", "Edit clinical visit documentation.", "تعديل التوثيق الطبي للزيارة."),
        new(VisitSession, "Visits", "Manage multiple doctor sessions within one visit.", "إدارة جلسات متعددة للأطباء داخل نفس الزيارة."),
        new(PrescriptionCreate, "ClinicalOrders", "Create prescriptions and clinical orders including medications, investigations, radiology, procedures and files.", "إنشاء الوصفات والأوامر الطبية بما يشمل الأدوية والفحوصات والأشعة والإجراءات والملفات."),
        new(DocumentationComplete, "Visits", "Mark clinical documentation as completed.", "تأكيد اكتمال التوثيق الطبي للزيارة."),
        new(FollowUpView, "FollowUps", "View patient follow-up recommendations.", "عرض توصيات المتابعة الخاصة بالمرضى."),
        new(ReportsView, "Reports", "View clinic reports and KPIs.", "عرض تقارير ومؤشرات أداء العيادة."),
        new(UsersManage, "Administration", "Create and edit clinic user accounts and role assignments.", "إنشاء وتعديل حسابات مستخدمي العيادة وتعيين الأدوار."),
        new(UsersManageStatus, "Administration", "Enable, disable or lock clinic user accounts according to account-management rules.", "تفعيل أو تعطيل أو إيقاف حسابات مستخدمي العيادة وفق قواعد إدارة الحسابات."),
        new(RbacView, "Administration", "View system roles and the complete permission matrix.", "عرض أدوار النظام ومصفوفة الصلاحيات كاملة."),
        new(ConfigManage, "Administration", "Manage clinic workflow, profile fields, clinical fields and order configuration.", "إدارة مسار العمل وحقول الملف الطبي والحقول السريرية وإعدادات الأوامر الطبية."),
        new(AuditView, "Administration", "View clinical and administrative audit history.", "عرض سجل التدقيق للعمليات الطبية والإدارية."),
        new(SettingsManage, "Administration", "Manage clinic identity, timezone, localization and document settings.", "إدارة هوية العيادة والمنطقة الزمنية واللغات وإعدادات المستندات.")
    ];
}
