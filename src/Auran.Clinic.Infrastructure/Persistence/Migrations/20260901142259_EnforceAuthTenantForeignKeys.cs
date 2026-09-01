using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAuthTenantForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Users_Id_ClinicId",
                table: "Users",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_ClinicId",
                table: "UserRoles",
                columns: new[] { "UserId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ClinicId",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ClinicId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Users_UserId_ClinicId",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId_ClinicId",
                table: "UserRoles",
                columns: new[] { "UserId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Users_UserId_ClinicId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId_ClinicId",
                table: "UserRoles");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Users_Id_ClinicId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_ClinicId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_ClinicId",
                table: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }
    }
}
