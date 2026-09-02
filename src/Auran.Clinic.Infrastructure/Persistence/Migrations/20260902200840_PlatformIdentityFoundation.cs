using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlatformIdentityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_AspNetUsers_IdentityUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_IdentityUserId",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "IdentityAccountType",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Clinic");

            migrationBuilder.AddColumn<string>(
                name: "AccountType",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Clinic");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AspNetUsers_Id_AccountType",
                table: "AspNetUsers",
                columns: new[] { "Id", "AccountType" });

            migrationBuilder.CreateTable(
                name: "PlatformUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentityUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IdentityAccountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Platform"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreateByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUsers", x => x.Id);
                    table.CheckConstraint("CK_PlatformUsers_IdentityAccountType", "[IdentityAccountType] = 'Platform'");
                    table.ForeignKey(
                        name: "FK_PlatformUsers_AspNetUsers_IdentityUserId_IdentityAccountType",
                        columns: x => new { x.IdentityUserId, x.IdentityAccountType },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "Id", "AccountType" },
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

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdentityUserId_IdentityAccountType",
                table: "Users",
                columns: new[] { "IdentityUserId", "IdentityAccountType" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_IdentityAccountType",
                table: "Users",
                sql: "[IdentityAccountType] = 'Clinic'");

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
                name: "IX_PlatformUsers_Email",
                table: "PlatformUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUsers_IdentityUserId_IdentityAccountType",
                table: "PlatformUsers",
                columns: new[] { "IdentityUserId", "IdentityAccountType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_AspNetUsers_IdentityUserId_IdentityAccountType",
                table: "Users",
                columns: new[] { "IdentityUserId", "IdentityAccountType" },
                principalTable: "AspNetUsers",
                principalColumns: new[] { "Id", "AccountType" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_AspNetUsers_IdentityUserId_IdentityAccountType",
                table: "Users");

            migrationBuilder.DropTable(
                name: "PlatformRefreshTokens");

            migrationBuilder.DropTable(
                name: "PlatformUsers");

            migrationBuilder.DropIndex(
                name: "IX_Users_IdentityUserId_IdentityAccountType",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_IdentityAccountType",
                table: "Users");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AspNetUsers_Id_AccountType",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IdentityAccountType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdentityUserId",
                table: "Users",
                column: "IdentityUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_AspNetUsers_IdentityUserId",
                table: "Users",
                column: "IdentityUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
