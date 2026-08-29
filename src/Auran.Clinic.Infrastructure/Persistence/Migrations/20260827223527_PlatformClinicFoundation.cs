using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlatformClinicFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the legacy audit actor column long enough to backfill the new
            // actor snapshot fields. Dropping only the FK here preserves the data.
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_ActorUserId",
                table: "AuditLogs");

            // Global role codes must be duplicated per existing clinic, so the old
            // global unique indexes have to be removed before the data conversion.
            migrationBuilder.DropIndex(
                name: "IX_Roles_Code",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions");

            migrationBuilder.RenameColumn(
                name: "IsSuperUser",
                table: "Users",
                newName: "IsClinicSuperUser");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Add these as nullable first. Legacy roles are global and cannot be
            // assigned Guid.Empty because the final columns reference Clinics.
            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "Roles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "RolePermissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Permissions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Group",
                table: "Permissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "Permissions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Clinic");

            // Existing clinics pre-date suspension support and must remain usable.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Clinics",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "AuditLogs",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClinicId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ActorDisplayName",
                table: "AuditLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorEmail",
                table: "AuditLogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActorId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorIdentityUserId",
                table: "AuditLogs",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorType",
                table: "AuditLogs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Clinic");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "AuditLogs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Clinic");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AuditLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            // Identity accounts that already exist are clinic accounts. Platform
            // identities are introduced by this migration and bootstrap later.
            migrationBuilder.AddColumn<string>(
                name: "AccountType",
                table: "AspNetUsers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Clinic");

            migrationBuilder.Sql("""
                UPDATE [Clinics]
                SET [IsActive] = 1;

                UPDATE [Permissions]
                SET [Scope] = 'Clinic';

                UPDATE [AspNetUsers]
                SET [AccountType] = 'Clinic';

                UPDATE auditLog
                SET [ActorId] = auditLog.[ActorUserId],
                    [ActorIdentityUserId] = clinicUser.[IdentityUserId],
                    [ActorDisplayName] = clinicUser.[FullName],
                    [ActorEmail] = clinicUser.[Email],
                    [ActorType] = 'Clinic',
                    [Scope] = 'Clinic',
                    [Category] = LEFT(COALESCE(NULLIF(auditLog.[EntityType], ''), 'Legacy'), 100)
                FROM [AuditLogs] auditLog
                LEFT JOIN [Users] clinicUser ON clinicUser.[Id] = auditLog.[ActorUserId];
                """);

            // Convert each legacy global role into a clinic-scoped copy for every
            // existing clinic. This preserves global-role availability for old
            // tenants, remaps UserRoles, and duplicates the permission mappings.
            migrationBuilder.Sql("""
                CREATE TABLE #RoleMap
                (
                    [LegacyRoleId] uniqueidentifier NOT NULL,
                    [ClinicId] uniqueidentifier NOT NULL,
                    [NewRoleId] uniqueidentifier NOT NULL,
                    PRIMARY KEY ([LegacyRoleId], [ClinicId])
                );

                INSERT INTO #RoleMap ([LegacyRoleId], [ClinicId], [NewRoleId])
                SELECT roleEntity.[Id], clinic.[Id], NEWID()
                FROM [Roles] roleEntity
                CROSS JOIN [Clinics] clinic
                WHERE roleEntity.[ClinicId] IS NULL;

                INSERT INTO [Roles]
                (
                    [Id], [Code], [Name], [IsSystem], [CreatedDate], [UpdatedDate],
                    [CreateByUserId], [UpdatedByUserId], [ClinicId]
                )
                SELECT mapping.[NewRoleId], roleEntity.[Code], roleEntity.[Name], roleEntity.[IsSystem],
                       roleEntity.[CreatedDate], roleEntity.[UpdatedDate], roleEntity.[CreateByUserId],
                       roleEntity.[UpdatedByUserId], mapping.[ClinicId]
                FROM #RoleMap mapping
                INNER JOIN [Roles] roleEntity ON roleEntity.[Id] = mapping.[LegacyRoleId];

                INSERT INTO [RolePermissions]
                (
                    [Id], [RoleId], [PermissionId], [CreatedDate], [UpdatedDate],
                    [CreateByUserId], [UpdatedByUserId], [ClinicId]
                )
                SELECT NEWID(), mapping.[NewRoleId], rolePermission.[PermissionId],
                       rolePermission.[CreatedDate], rolePermission.[UpdatedDate],
                       rolePermission.[CreateByUserId], rolePermission.[UpdatedByUserId], mapping.[ClinicId]
                FROM #RoleMap mapping
                INNER JOIN [RolePermissions] rolePermission
                    ON rolePermission.[RoleId] = mapping.[LegacyRoleId]
                WHERE rolePermission.[ClinicId] IS NULL;

                UPDATE userRole
                SET [RoleId] = mapping.[NewRoleId]
                FROM [UserRoles] userRole
                INNER JOIN #RoleMap mapping
                    ON mapping.[LegacyRoleId] = userRole.[RoleId]
                   AND mapping.[ClinicId] = userRole.[ClinicId];

                IF EXISTS
                (
                    SELECT 1
                    FROM [UserRoles] userRole
                    INNER JOIN [Roles] roleEntity ON roleEntity.[Id] = userRole.[RoleId]
                    WHERE roleEntity.[ClinicId] IS NULL
                )
                    THROW 51010, 'Unable to map one or more legacy UserRoles to a clinic-scoped role.', 1;

                DELETE FROM [RolePermissions]
                WHERE [ClinicId] IS NULL;

                DELETE FROM [Roles]
                WHERE [ClinicId] IS NULL;

                DROP TABLE #RoleMap;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClinicId",
                table: "Roles",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClinicId",
                table: "RolePermissions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Legacy actor data has been copied into snapshots; only now is it safe
            // to remove the old FK/index/column.
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ClinicId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                table: "AuditLogs");

            migrationBuilder.CreateTable(
                name: "FeatureDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDefaultEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformUsers_AspNetUsers_IdentityUserId",
                        column: x => x.IdentityUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicFeatures_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicFeatures_FeatureDefinitions_FeatureDefinitionId",
                        column: x => x.FeatureDefinitionId,
                        principalTable: "FeatureDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformRolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformRolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformRolePermissions_PlatformRoles_PlatformRoleId",
                        column: x => x.PlatformRoleId,
                        principalTable: "PlatformRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformRefreshTokens_PlatformUsers_PlatformUserId",
                        column: x => x.PlatformUserId,
                        principalTable: "PlatformUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformUserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformUserRoles_PlatformRoles_PlatformRoleId",
                        column: x => x.PlatformRoleId,
                        principalTable: "PlatformRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformUserRoles_PlatformUsers_PlatformUserId",
                        column: x => x.PlatformUserId,
                        principalTable: "PlatformUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ClinicId_Code",
                table: "Roles",
                columns: new[] { "ClinicId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_ClinicId_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "ClinicId", "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Scope",
                table: "Permissions",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorType_ActorId",
                table: "AuditLogs",
                columns: new[] { "ActorType", "ActorId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ClinicId_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "ClinicId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Scope_OccurredAtUtc",
                table: "AuditLogs",
                columns: new[] { "Scope", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AccountType",
                table: "AspNetUsers",
                column: "AccountType");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicFeatures_ClinicId_FeatureDefinitionId",
                table: "ClinicFeatures",
                columns: new[] { "ClinicId", "FeatureDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicFeatures_FeatureDefinitionId",
                table: "ClinicFeatures",
                column: "FeatureDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureDefinitions_Code",
                table: "FeatureDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRefreshTokens_PlatformUserId_ExpiresDate",
                table: "PlatformRefreshTokens",
                columns: new[] { "PlatformUserId", "ExpiresDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRefreshTokens_TokenHash",
                table: "PlatformRefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRolePermissions_PermissionId",
                table: "PlatformRolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRolePermissions_PlatformRoleId_PermissionId",
                table: "PlatformRolePermissions",
                columns: new[] { "PlatformRoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_Code",
                table: "PlatformRoles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUserRoles_PlatformRoleId",
                table: "PlatformUserRoles",
                column: "PlatformRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUserRoles_PlatformUserId_PlatformRoleId",
                table: "PlatformUserRoles",
                columns: new[] { "PlatformUserId", "PlatformRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUsers_Email",
                table: "PlatformUsers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUsers_IdentityUserId",
                table: "PlatformUsers",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Clinics_ClinicId",
                table: "RolePermissions",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Clinics_ClinicId",
                table: "Roles",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rolling clinic-scoped roles back to a global catalog is ambiguous when
            // the same code exists in more than one clinic. Refuse to silently lose
            // data. Likewise, the legacy AuditLog schema requires a valid clinic user
            // actor and non-null ClinicId for every row.
            migrationBuilder.Sql("""
                IF EXISTS
                (
                    SELECT [Code]
                    FROM [Roles]
                    GROUP BY [Code]
                    HAVING COUNT(*) > 1
                )
                    THROW 51020, 'Cannot roll back PlatformClinicFoundation because clinic-scoped role codes would collapse into duplicate global role codes.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM [AuditLogs] auditLog
                    WHERE auditLog.[ClinicId] IS NULL
                       OR auditLog.[ActorType] <> 'Clinic'
                       OR auditLog.[ActorId] IS NULL
                       OR NOT EXISTS
                       (
                           SELECT 1
                           FROM [Users] clinicUser
                           WHERE clinicUser.[Id] = auditLog.[ActorId]
                       )
                )
                    THROW 51021, 'Cannot roll back PlatformClinicFoundation because one or more audit rows cannot be represented by the legacy clinic-user actor schema.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Clinics_ClinicId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Clinics_ClinicId",
                table: "Roles");

            migrationBuilder.DropTable(
                name: "ClinicFeatures");

            migrationBuilder.DropTable(
                name: "PlatformRefreshTokens");

            migrationBuilder.DropTable(
                name: "PlatformRolePermissions");

            migrationBuilder.DropTable(
                name: "PlatformUserRoles");

            migrationBuilder.DropTable(
                name: "FeatureDefinitions");

            migrationBuilder.DropTable(
                name: "PlatformRoles");

            migrationBuilder.DropTable(
                name: "PlatformUsers");

            migrationBuilder.DropIndex(
                name: "IX_Roles_ClinicId_Code",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_ClinicId_RoleId_PermissionId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Scope",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActorType_ActorId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ClinicId_OccurredAtUtc",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Scope_OccurredAtUtc",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AccountType",
                table: "AspNetUsers");

            // Restore ActorUserId from the preserved actor id before removing the
            // expanded actor metadata columns.
            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [AuditLogs]
                SET [ActorUserId] = [ActorId];
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorUserId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "ActorDisplayName",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorEmail",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorIdentityUserId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorType",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "IsClinicSuperUser",
                table: "Users",
                newName: "IsSuperUser");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Group",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(160)",
                oldMaxLength: 160);

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClinicId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(160)",
                oldMaxLength: 160);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ClinicId",
                table: "AuditLogs",
                column: "ClinicId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
