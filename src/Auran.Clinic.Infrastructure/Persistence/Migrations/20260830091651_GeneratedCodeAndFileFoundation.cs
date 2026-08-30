using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeneratedCodeAndFileFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Users_UploadedByUserId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_FileUploadSessions_Users_RequestedByUserId",
                table: "FileUploadSessions");

            migrationBuilder.DropIndex(
                name: "IX_FileUploadSessions_RequestedByUserId",
                table: "FileUploadSessions");

            migrationBuilder.DropIndex(
                name: "IX_Files_UploadedByUserId",
                table: "Files");

            migrationBuilder.RenameColumn(
                name: "RequestedByUserId",
                table: "FileUploadSessions",
                newName: "RequestedByActorId");

            migrationBuilder.RenameColumn(
                name: "UploadedByUserId",
                table: "Files",
                newName: "UploadedByActorId");

            migrationBuilder.AddColumn<string>(
                name: "RequestedByActorType",
                table: "FileUploadSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UploadedByActorType",
                table: "Files",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploadSessions_RequestedByActorType_RequestedByActorId",
                table: "FileUploadSessions",
                columns: new[] { "RequestedByActorType", "RequestedByActorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_UploadedByActorType_UploadedByActorId",
                table: "Files",
                columns: new[] { "UploadedByActorType", "UploadedByActorId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileUploadSessions_RequestedByActorType_RequestedByActorId",
                table: "FileUploadSessions");

            migrationBuilder.DropIndex(
                name: "IX_Files_UploadedByActorType_UploadedByActorId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "RequestedByActorType",
                table: "FileUploadSessions");

            migrationBuilder.DropColumn(
                name: "UploadedByActorType",
                table: "Files");

            migrationBuilder.RenameColumn(
                name: "RequestedByActorId",
                table: "FileUploadSessions",
                newName: "RequestedByUserId");

            migrationBuilder.RenameColumn(
                name: "UploadedByActorId",
                table: "Files",
                newName: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileUploadSessions_RequestedByUserId",
                table: "FileUploadSessions",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_UploadedByUserId",
                table: "Files",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Users_UploadedByUserId",
                table: "Files",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FileUploadSessions_Users_RequestedByUserId",
                table: "FileUploadSessions",
                column: "RequestedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
