using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BusinessCodeAndFileStorageFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The existing Files table previously allowed unconstrained strings. Fail with
            // an actionable error rather than silently truncating data or creating an
            // invalid unique index during the storage foundation upgrade.
            migrationBuilder.Sql("""
                IF EXISTS
                (
                    SELECT 1
                    FROM [Files]
                    WHERE LEN([StoredName]) > 255
                       OR LEN([StorageKey]) > 500
                       OR LEN([OriginalName]) > 255
                       OR LEN([ContentType]) > 200
                )
                    THROW 51000, 'Existing file metadata exceeds the supported storage lengths.', 1;

                IF EXISTS
                (
                    SELECT [StorageKey]
                    FROM [Files]
                    GROUP BY [StorageKey]
                    HAVING COUNT(*) > 1
                )
                    THROW 51001, 'Existing file records contain duplicate StorageKey values.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM [Files]
                    WHERE [StorageProvider] NOT IN ('Local', 'S3')
                )
                    THROW 51002, 'Existing file records contain an unsupported StorageProvider.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Files_ClinicId",
                table: "Files");

            migrationBuilder.AlterColumn<string>(
                name: "StoredName",
                table: "Files",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "StorageProvider",
                table: "Files",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "StorageKey",
                table: "Files",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "OriginalName",
                table: "Files",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "Files",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "FileExtension",
                table: "Files",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Preserve useful metadata for any legacy rows without making the migration
            // depend on a particular storage provider or physical path format.
            migrationBuilder.Sql("""
                UPDATE [Files]
                SET [FileExtension] =
                    CASE
                        WHEN CHARINDEX('.', REVERSE([OriginalName])) BETWEEN 2 AND 20
                            THEN LOWER(RIGHT([OriginalName], CHARINDEX('.', REVERSE([OriginalName]))))
                        ELSE ''
                    END
                WHERE [FileExtension] = '';
                """);

            migrationBuilder.AddColumn<string>(
                name: "CityCode",
                table: "ClinicSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "ClinicSettings",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CodeCounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeCounters", x => x.Id);
                    table.CheckConstraint("CK_CodeCounters_ScopeClinic", "([Scope] = 'Platform' AND [ClinicId] IS NULL) OR ([Scope] = 'Clinic' AND [ClinicId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CodeCounters_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileUploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpectedSize = table.Column<long>(type: "bigint", nullable: false),
                    StorageProvider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileUploadSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileUploadSessions_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileUploadSessions_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileUploadSessions_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Files_ClinicId_UploadedAtUtc",
                table: "Files",
                columns: new[] { "ClinicId", "UploadedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_StorageKey",
                table: "Files",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeCounters_ClinicId_CodeType_Prefix_Year",
                table: "CodeCounters",
                columns: new[] { "ClinicId", "CodeType", "Prefix", "Year" },
                unique: true,
                filter: "[ClinicId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CodeCounters_Scope_CodeType_Prefix_Year",
                table: "CodeCounters",
                columns: new[] { "Scope", "CodeType", "Prefix", "Year" },
                unique: true,
                filter: "[ClinicId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploadSessions_ClinicId_Status_ExpiresAtUtc",
                table: "FileUploadSessions",
                columns: new[] { "ClinicId", "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FileUploadSessions_FileId",
                table: "FileUploadSessions",
                column: "FileId",
                unique: true,
                filter: "[FileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploadSessions_RequestedByUserId",
                table: "FileUploadSessions",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploadSessions_UploadTokenHash",
                table: "FileUploadSessions",
                column: "UploadTokenHash",
                unique: true);

            // Existing clinics were provisioned before file permissions existed. Seed the
            // global permission catalog entries and grant them to the protected system
            // roles so upgrades behave the same as newly provisioned clinics.
            migrationBuilder.Sql("""
                DECLARE @Now datetime2 = SYSUTCDATETIME();
                DECLARE @FilesViewId uniqueidentifier =
                    (SELECT TOP (1) [Id] FROM [Permissions] WHERE [Code] = 'Files.View');
                DECLARE @FilesUploadId uniqueidentifier =
                    (SELECT TOP (1) [Id] FROM [Permissions] WHERE [Code] = 'Files.Upload');

                IF @FilesViewId IS NULL
                BEGIN
                    SET @FilesViewId = NEWID();
                    INSERT INTO [Permissions]
                        ([Id], [Code], [Name], [Group], [Scope], [CreatedDate], [UpdatedDate], [CreateByUserId], [UpdatedByUserId])
                    VALUES
                        (@FilesViewId, 'Files.View', 'View and download files', 'Files', 'Clinic', @Now, NULL, NULL, NULL);
                END
                ELSE
                BEGIN
                    UPDATE [Permissions]
                    SET [Name] = 'View and download files', [Group] = 'Files', [Scope] = 'Clinic'
                    WHERE [Id] = @FilesViewId;
                END

                IF @FilesUploadId IS NULL
                BEGIN
                    SET @FilesUploadId = NEWID();
                    INSERT INTO [Permissions]
                        ([Id], [Code], [Name], [Group], [Scope], [CreatedDate], [UpdatedDate], [CreateByUserId], [UpdatedByUserId])
                    VALUES
                        (@FilesUploadId, 'Files.Upload', 'Upload files', 'Files', 'Clinic', @Now, NULL, NULL, NULL);
                END
                ELSE
                BEGIN
                    UPDATE [Permissions]
                    SET [Name] = 'Upload files', [Group] = 'Files', [Scope] = 'Clinic'
                    WHERE [Id] = @FilesUploadId;
                END

                INSERT INTO [RolePermissions]
                    ([Id], [RoleId], [PermissionId], [CreatedDate], [UpdatedDate], [CreateByUserId], [UpdatedByUserId], [ClinicId])
                SELECT NEWID(), r.[Id], p.[PermissionId], @Now, NULL, NULL, NULL, r.[ClinicId]
                FROM [Roles] r
                CROSS APPLY (VALUES (@FilesViewId), (@FilesUploadId)) p([PermissionId])
                WHERE r.[IsSystem] = 1
                  AND r.[Code] IN ('ADMIN', 'RECEPTIONIST', 'DOCTOR', 'NURSE')
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [RolePermissions] rp
                      WHERE rp.[RoleId] = r.[Id]
                        AND rp.[PermissionId] = p.[PermissionId]
                        AND rp.[ClinicId] = r.[ClinicId]
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE rp
                FROM [RolePermissions] rp
                INNER JOIN [Permissions] p ON p.[Id] = rp.[PermissionId]
                WHERE p.[Code] IN ('Files.View', 'Files.Upload');

                DELETE FROM [Permissions]
                WHERE [Code] IN ('Files.View', 'Files.Upload');
                """);

            migrationBuilder.DropTable(
                name: "CodeCounters");

            migrationBuilder.DropTable(
                name: "FileUploadSessions");

            migrationBuilder.DropIndex(
                name: "IX_Files_ClinicId_UploadedAtUtc",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_StorageKey",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "FileExtension",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "CityCode",
                table: "ClinicSettings");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "ClinicSettings");

            migrationBuilder.AlterColumn<string>(
                name: "StoredName",
                table: "Files",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "StorageProvider",
                table: "Files",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "StorageKey",
                table: "Files",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "OriginalName",
                table: "Files",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "Files",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_Files_ClinicId",
                table: "Files",
                column: "ClinicId");
        }
    }
}
