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
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "IX_VisitSessions_ClinicId",
                table: "VisitSessions");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_ClinicId_VisitId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileValues_ClinicId",
                table: "PatientProfileValues");

            migrationBuilder.DropColumn(
                name: "FollowUpText",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "JsonValue",
                table: "PatientProfileValues");

            migrationBuilder.RenameColumn(
                name: "Group",
                table: "Permissions",
                newName: "GroupKey");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Permissions",
                newName: "Key");

            migrationBuilder.RenameIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                newName: "IX_Permissions_Key");

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
                defaultValue: false);

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

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValueOptions_OptionId",
                table: "PatientProfileValueOptions",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValueOptions_PatientProfileValueId",
                table: "PatientProfileValueOptions",
                column: "PatientProfileValueId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTranslations_PermissionId_LanguageCode",
                table: "PermissionTranslations",
                columns: new[] { "PermissionId", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientProfileValueOptions");

            migrationBuilder.DropTable(
                name: "PermissionTranslations");

            migrationBuilder.DropIndex(
                name: "IX_VisitSessions_ClinicId_VisitId",
                table: "VisitSessions");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_ClinicId_VisitId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileValues_ClinicId_PatientId_FieldId",
                table: "PatientProfileValues");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "Permissions",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "GroupKey",
                table: "Permissions",
                newName: "Group");

            migrationBuilder.RenameIndex(
                name: "IX_Permissions_Key",
                table: "Permissions",
                newName: "IX_Permissions_Code");

            migrationBuilder.AddColumn<string>(
                name: "FollowUpText",
                table: "Visits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Permissions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JsonValue",
                table: "PatientProfileValues",
                type: "nvarchar(max)",
                nullable: true);

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
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_ClinicId",
                table: "VisitSessions",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_ClinicId_VisitId",
                table: "QueueEntries",
                columns: new[] { "ClinicId", "VisitId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValues_ClinicId",
                table: "PatientProfileValues",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");
        }
    }
}
