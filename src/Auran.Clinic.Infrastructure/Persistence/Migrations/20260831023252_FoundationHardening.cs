using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FoundationHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AspNetRoleClaims");
            migrationBuilder.DropTable(name: "AspNetUserRoles");
            migrationBuilder.DropTable(name: "AspNetRoles");

            migrationBuilder.DropIndex(name: "IX_VisitSessions_ClinicId", table: "VisitSessions");
            migrationBuilder.DropIndex(name: "IX_QueueEntries_ClinicId_VisitId", table: "QueueEntries");
            migrationBuilder.DropIndex(name: "IX_PatientProfileValues_ClinicId", table: "PatientProfileValues");

            migrationBuilder.DropColumn(name: "FollowUpText", table: "Visits");
            migrationBuilder.DropColumn(name: "Name", table: "Permissions");
            migrationBuilder.DropColumn(name: "JsonValue", table: "PatientProfileValues");

            migrationBuilder.RenameColumn(name: "Group", table: "Permissions", newName: "GroupKey");
            migrationBuilder.RenameColumn(name: "Code", table: "Permissions", newName: "Key");
            migrationBuilder.RenameIndex(name: "IX_Permissions_Code", table: "Permissions", newName: "IX_Permissions_Key");

            migrationBuilder.Sql("""
                UPDATE [Permissions]
                SET [Key] = CASE [Key]
                    WHEN 'Platform.Clinics.View' THEN 'Platform_Clinics_View'
                    WHEN 'Platform.Clinics.Create' THEN 'Platform_Clinics_Create'
                    WHEN 'Platform.Clinics.Update' THEN 'Platform_Clinics_Update'
                    WHEN 'Platform.Clinics.SetStatus' THEN 'Platform_Clinics_Set_Status'
                    WHEN 'Platform.Clinics.Features.Manage' THEN 'Platform_Clinics_Manage_Features'
                    WHEN 'Platform.AuditLogs.View' THEN 'Platform_Audit_View'
                    WHEN 'Platform.Users.Manage' THEN 'Platform_Users_Manage'
                    WHEN 'AuditLogs.View' THEN 'Audit_View'
                    WHEN 'Patients.View' THEN 'Patient_View'
                    WHEN 'Patients.Create' THEN 'Patient_Create'
                    WHEN 'Patients.Update' THEN 'Patient_Edit_Basic'
                    WHEN 'Users.View' THEN 'Users_View'
                    WHEN 'Users.Manage' THEN 'Users_Manage'
                    WHEN 'Roles.View' THEN 'RBAC_View'
                    WHEN 'Roles.Manage' THEN 'RBAC_Manage'
                    WHEN 'Queue.View' THEN 'Queue_View'
                    WHEN 'Queue.Manage' THEN 'Queue_Move'
                    WHEN 'Visits.View' THEN 'Visit_View'
                    WHEN 'Visits.Create' THEN 'Visit_Start'
                    WHEN 'Visits.Update' THEN 'Visit_Edit'
                    WHEN 'Clinical.View' THEN 'MedicalProfile_View'
                    WHEN 'Clinical.Manage' THEN 'MedicalProfile_Edit'
                    WHEN 'FollowUps.View' THEN 'FollowUp_View'
                    WHEN 'FollowUps.Manage' THEN 'FollowUp_Manage'
                    WHEN 'Reports.View' THEN 'Reports_View'
                    WHEN 'Reports.Export' THEN 'Reports_Export'
                    WHEN 'Settings.View' THEN 'Settings_View'
                    WHEN 'Settings.Manage' THEN 'Settings_Manage'
                    WHEN 'Files.View' THEN 'Files_View'
                    WHEN 'Files.Upload' THEN 'Files_Upload'
                    ELSE [Key]
                END;

                UPDATE [Permissions]
                SET [GroupKey] = CASE
                    WHEN [Key] LIKE 'Platform_Clinics_%' THEN 'Platform Clinics'
                    WHEN [Key] = 'Platform_Audit_View' THEN 'Platform Audit'
                    WHEN [Key] = 'Platform_Users_Manage' THEN 'Platform Users'
                    WHEN [Key] IN ('Audit_View', 'Users_View', 'Users_Manage', 'Users_Manage_Status', 'RBAC_View', 'RBAC_Manage', 'Settings_View', 'Settings_Manage') THEN 'Administration'
                    WHEN [Key] IN ('Patient_View', 'Patient_Create', 'Patient_Edit_Basic') THEN 'Patients'
                    WHEN [Key] IN ('Queue_View', 'Queue_Move') THEN 'Clinic Workflow'
                    WHEN [Key] IN ('Visit_View', 'Visit_Start', 'Visit_Edit') THEN 'Visits'
                    WHEN [Key] IN ('MedicalProfile_View', 'MedicalProfile_Edit') THEN 'Clinical'
                    WHEN [Key] IN ('FollowUp_View', 'FollowUp_Manage') THEN 'Follow Ups'
                    WHEN [Key] IN ('Reports_View', 'Reports_Export') THEN 'Reports'
                    WHEN [Key] IN ('Files_View', 'Files_Upload') THEN 'Files'
                    ELSE [GroupKey]
                END;

                IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [Key] = 'Users_Manage_Status')
                BEGIN
                    INSERT INTO [Permissions]
                        ([Id], [Key], [GroupKey], [Scope], [CreatedDate], [UpdatedDate], [CreateByUserId], [UpdatedByUserId])
                    VALUES
                        (NEWID(), 'Users_Manage_Status', 'Administration', 'Clinic', SYSUTCDATETIME(), NULL, NULL, NULL);
                END;
                """);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Visits",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<long>(
                name: "LastNumber",
                table: "CodeCounters",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "PatientProfileValueOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientProfileValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientProfileValueOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientProfileValueOptions_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientProfileValueOptions_PatientProfileFieldOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "PatientProfileFieldOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientProfileValueOptions_PatientProfileValues_PatientProfileValueId",
                        column: x => x.PatientProfileValueId,
                        principalTable: "PatientProfileValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermissionTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionTranslations_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO [PermissionTranslations]
                    ([Id], [PermissionId], [LanguageCode], [Description], [CreatedDate], [UpdatedDate], [CreateByUserId], [UpdatedByUserId])
                SELECT NEWID(), p.[Id], 'en',
                    CASE p.[Key]
                        WHEN 'Platform_Clinics_View' THEN 'View clinics.'
                        WHEN 'Platform_Clinics_Create' THEN 'Create and provision clinics.'
                        WHEN 'Platform_Clinics_Update' THEN 'Update clinic information.'
                        WHEN 'Platform_Clinics_Set_Status' THEN 'Activate or suspend a clinic and all clinic access.'
                        WHEN 'Platform_Clinics_Manage_Features' THEN 'Manage clinic feature availability.'
                        WHEN 'Platform_Audit_View' THEN 'View platform audit history.'
                        WHEN 'Platform_Users_Manage' THEN 'Manage platform users.'
                        WHEN 'Audit_View' THEN 'View clinic audit history.'
                        WHEN 'Patient_View' THEN 'Search and view patient profiles.'
                        WHEN 'Patient_Create' THEN 'Create a new patient record.'
                        WHEN 'Patient_Edit_Basic' THEN 'Edit patient basic information.'
                        WHEN 'Users_View' THEN 'View clinic users.'
                        WHEN 'Users_Manage' THEN 'Create and edit clinic users and role assignments.'
                        WHEN 'Users_Manage_Status' THEN 'Enable or disable clinic user accounts.'
                        WHEN 'RBAC_View' THEN 'View roles and permission assignments.'
                        WHEN 'RBAC_Manage' THEN 'Manage role permission assignments.'
                        WHEN 'Queue_View' THEN 'View the live clinic queue.'
                        WHEN 'Queue_Move' THEN 'Move patients through allowed workflow transitions.'
                        WHEN 'Visit_View' THEN 'View visits and sessions.'
                        WHEN 'Visit_Start' THEN 'Start a visit or doctor session.'
                        WHEN 'Visit_Edit' THEN 'Edit visit documentation.'
                        WHEN 'MedicalProfile_View' THEN 'View patient clinical information.'
                        WHEN 'MedicalProfile_Edit' THEN 'Edit patient clinical information and measurements.'
                        WHEN 'FollowUp_View' THEN 'View patient follow-up recommendations.'
                        WHEN 'FollowUp_Manage' THEN 'Manage patient follow-up recommendations.'
                        WHEN 'Reports_View' THEN 'View clinic reports and KPIs.'
                        WHEN 'Reports_Export' THEN 'Export permitted clinic reports.'
                        WHEN 'Settings_View' THEN 'View clinic settings.'
                        WHEN 'Settings_Manage' THEN 'Manage clinic identity, localization and settings.'
                        WHEN 'Files_View' THEN 'View and download authorized clinic files.'
                        WHEN 'Files_Upload' THEN 'Upload files to the clinic file registry.'
                    END,
                    SYSUTCDATETIME(), NULL, NULL, NULL
                FROM [Permissions] p
                WHERE p.[Key] IN (
                    'Platform_Clinics_View','Platform_Clinics_Create','Platform_Clinics_Update','Platform_Clinics_Set_Status','Platform_Clinics_Manage_Features','Platform_Audit_View','Platform_Users_Manage',
                    'Audit_View','Patient_View','Patient_Create','Patient_Edit_Basic','Users_View','Users_Manage','Users_Manage_Status','RBAC_View','RBAC_Manage','Queue_View','Queue_Move','Visit_View','Visit_Start','Visit_Edit','MedicalProfile_View','MedicalProfile_Edit','FollowUp_View','FollowUp_Manage','Reports_View','Reports_Export','Settings_View','Settings_Manage','Files_View','Files_Upload')
                  AND NOT EXISTS (
                      SELECT 1 FROM [PermissionTranslations] t
                      WHERE t.[PermissionId] = p.[Id] AND t.[LanguageCode] = 'en');

                INSERT INTO [PermissionTranslations]
                    ([Id], [PermissionId], [LanguageCode], [Description], [CreatedDate], [UpdatedDate], [CreateByUserId], [UpdatedByUserId])
                SELECT NEWID(), p.[Id], 'ar',
                    CASE p.[Key]
                        WHEN 'Platform_Clinics_View' THEN N'عرض العيادات.'
                        WHEN 'Platform_Clinics_Create' THEN N'إنشاء وتجهيز العيادات.'
                        WHEN 'Platform_Clinics_Update' THEN N'تعديل بيانات العيادة.'
                        WHEN 'Platform_Clinics_Set_Status' THEN N'تفعيل أو إيقاف العيادة وكل الحسابات التابعة لها.'
                        WHEN 'Platform_Clinics_Manage_Features' THEN N'إدارة الخصائص المتاحة للعيادة.'
                        WHEN 'Platform_Audit_View' THEN N'عرض سجل تدقيق المنصة.'
                        WHEN 'Platform_Users_Manage' THEN N'إدارة مستخدمي المنصة.'
                        WHEN 'Audit_View' THEN N'عرض سجل التدقيق الخاص بالعيادة.'
                        WHEN 'Patient_View' THEN N'البحث عن المرضى وعرض ملفاتهم.'
                        WHEN 'Patient_Create' THEN N'إنشاء سجل مريض جديد.'
                        WHEN 'Patient_Edit_Basic' THEN N'تعديل البيانات الأساسية للمريض.'
                        WHEN 'Users_View' THEN N'عرض مستخدمي العيادة.'
                        WHEN 'Users_Manage' THEN N'إنشاء وتعديل مستخدمي العيادة وتعيين الأدوار.'
                        WHEN 'Users_Manage_Status' THEN N'تفعيل أو تعطيل حسابات مستخدمي العيادة.'
                        WHEN 'RBAC_View' THEN N'عرض الأدوار وتعيينات الصلاحيات.'
                        WHEN 'RBAC_Manage' THEN N'إدارة تعيين الصلاحيات للأدوار.'
                        WHEN 'Queue_View' THEN N'عرض قائمة انتظار العيادة المباشرة.'
                        WHEN 'Queue_Move' THEN N'نقل المرضى بين حالات مسار العمل المسموح بها.'
                        WHEN 'Visit_View' THEN N'عرض الزيارات والجلسات.'
                        WHEN 'Visit_Start' THEN N'بدء زيارة أو جلسة طبيب.'
                        WHEN 'Visit_Edit' THEN N'تعديل توثيق الزيارة.'
                        WHEN 'MedicalProfile_View' THEN N'عرض البيانات السريرية للمريض.'
                        WHEN 'MedicalProfile_Edit' THEN N'تعديل البيانات والقياسات السريرية للمريض.'
                        WHEN 'FollowUp_View' THEN N'عرض توصيات متابعة المرضى.'
                        WHEN 'FollowUp_Manage' THEN N'إدارة توصيات متابعة المرضى.'
                        WHEN 'Reports_View' THEN N'عرض تقارير ومؤشرات أداء العيادة.'
                        WHEN 'Reports_Export' THEN N'تصدير تقارير العيادة المسموح بها.'
                        WHEN 'Settings_View' THEN N'عرض إعدادات العيادة.'
                        WHEN 'Settings_Manage' THEN N'إدارة هوية العيادة واللغة والإعدادات.'
                        WHEN 'Files_View' THEN N'عرض وتنزيل ملفات العيادة المصرح بها.'
                        WHEN 'Files_Upload' THEN N'رفع الملفات إلى سجل ملفات العيادة.'
                    END,
                    SYSUTCDATETIME(), NULL, NULL, NULL
                FROM [Permissions] p
                WHERE p.[Key] IN (
                    'Platform_Clinics_View','Platform_Clinics_Create','Platform_Clinics_Update','Platform_Clinics_Set_Status','Platform_Clinics_Manage_Features','Platform_Audit_View','Platform_Users_Manage',
                    'Audit_View','Patient_View','Patient_Create','Patient_Edit_Basic','Users_View','Users_Manage','Users_Manage_Status','RBAC_View','RBAC_Manage','Queue_View','Queue_Move','Visit_View','Visit_Start','Visit_Edit','MedicalProfile_View','MedicalProfile_Edit','FollowUp_View','FollowUp_Manage','Reports_View','Reports_Export','Settings_View','Settings_Manage','Files_View','Files_Upload')
                  AND NOT EXISTS (
                      SELECT 1 FROM [PermissionTranslations] t
                      WHERE t.[PermissionId] = p.[Id] AND t.[LanguageCode] = 'ar');

                INSERT INTO [RolePermissions]
                    ([Id], [RoleId], [PermissionId], [CreatedDate], [UpdatedDate], [CreateByUserId], [UpdatedByUserId], [ClinicId])
                SELECT NEWID(), r.[Id], p.[Id], SYSUTCDATETIME(), NULL, NULL, NULL, r.[ClinicId]
                FROM [Roles] r
                CROSS JOIN [Permissions] p
                WHERE r.[Code] = 'ADMIN'
                  AND p.[Key] = 'Users_Manage_Status'
                  AND p.[Scope] = 'Clinic'
                  AND NOT EXISTS (
                      SELECT 1 FROM [RolePermissions] rp
                      WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id] AND rp.[ClinicId] = r.[ClinicId]);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_ClinicId_VisitId",
                table: "VisitSessions",
                columns: new[] { "ClinicId", "VisitId" },
                unique: true,
                filter: "[EndedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_ClinicId_VisitId",
                table: "QueueEntries",
                columns: new[] { "ClinicId", "VisitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValues_ClinicId_PatientId_FieldId",
                table: "PatientProfileValues",
                columns: new[] { "ClinicId", "PatientId", "FieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValueOptions_ClinicId_PatientProfileValueId_OptionId",
                table: "PatientProfileValueOptions",
                columns: new[] { "ClinicId", "PatientProfileValueId", "OptionId" },
                unique: true);

            migrationBuilder.CreateIndex(name: "IX_PatientProfileValueOptions_OptionId", table: "PatientProfileValueOptions", column: "OptionId");
            migrationBuilder.CreateIndex(name: "IX_PatientProfileValueOptions_PatientProfileValueId", table: "PatientProfileValueOptions", column: "PatientProfileValueId");
            migrationBuilder.CreateIndex(
                name: "IX_PermissionTranslations_PermissionId_LanguageCode",
                table: "PermissionTranslations",
                columns: new[] { "PermissionId", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE rp
                FROM [RolePermissions] rp
                INNER JOIN [Permissions] p ON p.[Id] = rp.[PermissionId]
                WHERE p.[Key] = 'Users_Manage_Status';

                DELETE FROM [Permissions] WHERE [Key] = 'Users_Manage_Status';

                UPDATE [Permissions]
                SET [Key] = CASE [Key]
                    WHEN 'Platform_Clinics_View' THEN 'Platform.Clinics.View'
                    WHEN 'Platform_Clinics_Create' THEN 'Platform.Clinics.Create'
                    WHEN 'Platform_Clinics_Update' THEN 'Platform.Clinics.Update'
                    WHEN 'Platform_Clinics_Set_Status' THEN 'Platform.Clinics.SetStatus'
                    WHEN 'Platform_Clinics_Manage_Features' THEN 'Platform.Clinics.Features.Manage'
                    WHEN 'Platform_Audit_View' THEN 'Platform.AuditLogs.View'
                    WHEN 'Platform_Users_Manage' THEN 'Platform.Users.Manage'
                    WHEN 'Audit_View' THEN 'AuditLogs.View'
                    WHEN 'Patient_View' THEN 'Patients.View'
                    WHEN 'Patient_Create' THEN 'Patients.Create'
                    WHEN 'Patient_Edit_Basic' THEN 'Patients.Update'
                    WHEN 'Users_View' THEN 'Users.View'
                    WHEN 'Users_Manage' THEN 'Users.Manage'
                    WHEN 'RBAC_View' THEN 'Roles.View'
                    WHEN 'RBAC_Manage' THEN 'Roles.Manage'
                    WHEN 'Queue_View' THEN 'Queue.View'
                    WHEN 'Queue_Move' THEN 'Queue.Manage'
                    WHEN 'Visit_View' THEN 'Visits.View'
                    WHEN 'Visit_Start' THEN 'Visits.Create'
                    WHEN 'Visit_Edit' THEN 'Visits.Update'
                    WHEN 'MedicalProfile_View' THEN 'Clinical.View'
                    WHEN 'MedicalProfile_Edit' THEN 'Clinical.Manage'
                    WHEN 'FollowUp_View' THEN 'FollowUps.View'
                    WHEN 'FollowUp_Manage' THEN 'FollowUps.Manage'
                    WHEN 'Reports_View' THEN 'Reports.View'
                    WHEN 'Reports_Export' THEN 'Reports.Export'
                    WHEN 'Settings_View' THEN 'Settings.View'
                    WHEN 'Settings_Manage' THEN 'Settings.Manage'
                    WHEN 'Files_View' THEN 'Files.View'
                    WHEN 'Files_Upload' THEN 'Files.Upload'
                    ELSE [Key]
                END;
                """);

            migrationBuilder.DropTable(name: "PatientProfileValueOptions");
            migrationBuilder.DropTable(name: "PermissionTranslations");

            migrationBuilder.DropIndex(name: "IX_VisitSessions_ClinicId_VisitId", table: "VisitSessions");
            migrationBuilder.DropIndex(name: "IX_QueueEntries_ClinicId_VisitId", table: "QueueEntries");
            migrationBuilder.DropIndex(name: "IX_PatientProfileValues_ClinicId_PatientId_FieldId", table: "PatientProfileValues");

            migrationBuilder.DropColumn(name: "RowVersion", table: "Visits");
            migrationBuilder.DropColumn(name: "IsActive", table: "Users");

            migrationBuilder.RenameColumn(name: "Key", table: "Permissions", newName: "Code");
            migrationBuilder.RenameColumn(name: "GroupKey", table: "Permissions", newName: "Group");
            migrationBuilder.RenameIndex(name: "IX_Permissions_Key", table: "Permissions", newName: "IX_Permissions_Code");

            migrationBuilder.AddColumn<string>(name: "FollowUpText", table: "Visits", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Name", table: "Permissions", type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "JsonValue", table: "PatientProfileValues", type: "nvarchar(max)", nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LastNumber",
                table: "CodeCounters",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AspNetRoles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetRoleClaims_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey("FK_AspNetUserRoles_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_AspNetUserRoles_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_VisitSessions_ClinicId", table: "VisitSessions", column: "ClinicId");
            migrationBuilder.CreateIndex(name: "IX_QueueEntries_ClinicId_VisitId", table: "QueueEntries", columns: new[] { "ClinicId", "VisitId" });
            migrationBuilder.CreateIndex(name: "IX_PatientProfileValues_ClinicId", table: "PatientProfileValues", column: "ClinicId");
            migrationBuilder.CreateIndex(name: "IX_AspNetRoleClaims_RoleId", table: "AspNetRoleClaims", column: "RoleId");
            migrationBuilder.CreateIndex(name: "RoleNameIndex", table: "AspNetRoles", column: "NormalizedName", unique: true, filter: "[NormalizedName] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_AspNetUserRoles_RoleId", table: "AspNetUserRoles", column: "RoleId");
        }
    }
}
