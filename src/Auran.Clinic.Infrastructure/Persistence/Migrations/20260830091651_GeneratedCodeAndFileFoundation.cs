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

            // Preserve the actor ids already stored by the previous file foundation.
            // Before this migration those ids could only reference clinic Users because
            // both columns had physical FKs to Users, so the safe legacy actor type is Clinic.
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
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedByActorType",
                table: "Files",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [FileUploadSessions]
                SET [RequestedByActorType] = 'Clinic'
                WHERE [RequestedByActorType] IS NULL;

                UPDATE [Files]
                SET [UploadedByActorType] = 'Clinic'
                WHERE [UploadedByActorType] IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "RequestedByActorType",
                table: "FileUploadSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UploadedByActorType",
                table: "Files",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

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
            // The old schema can represent only clinic User actors. Refuse to collapse
            // Platform actor rows into a clinic User FK because that would either corrupt
            // attribution or fail with a less actionable foreign-key error.
            migrationBuilder.Sql("""
                IF EXISTS
                (
                    SELECT 1
                    FROM [FileUploadSessions]
                    WHERE [RequestedByActorType] <> 'Clinic'
                )
                    THROW 51020, 'Cannot downgrade file upload sessions containing non-clinic actors.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM [Files]
                    WHERE [UploadedByActorType] <> 'Clinic'
                )
                    THROW 51021, 'Cannot downgrade files containing non-clinic upload actors.', 1;
                """);

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
