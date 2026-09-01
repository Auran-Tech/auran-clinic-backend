using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantTenantSingleForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Users_UploadedByUserId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientAllergies_Patients_PatientId",
                table: "PatientAllergies");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientAllergies_Users_RecordedByUserId",
                table: "PatientAllergies");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientAttachments_Files_FileId",
                table: "PatientAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientAttachments_Patients_PatientId",
                table: "PatientAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientConditions_Patients_PatientId",
                table: "PatientConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientConditions_Users_RecordedByUserId",
                table: "PatientConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedications_Patients_PatientId",
                table: "PatientMedications");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedications_Users_RecordedByUserId",
                table: "PatientMedications");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfileFieldOptions_PatientProfileFields_FieldId",
                table: "PatientProfileFieldOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfileFields_PatientProfileSections_SectionId",
                table: "PatientProfileFields");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfileValues_Files_FileId",
                table: "PatientProfileValues");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfileValues_PatientProfileFields_FieldId",
                table: "PatientProfileValues");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfileValues_Patients_PatientId",
                table: "PatientProfileValues");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Files_Users_UploadedByUserId",
                table: "Files",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAllergies_Patients_PatientId",
                table: "PatientAllergies",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAllergies_Users_RecordedByUserId",
                table: "PatientAllergies",
                column: "RecordedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAttachments_Files_FileId",
                table: "PatientAttachments",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAttachments_Patients_PatientId",
                table: "PatientAttachments",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientConditions_Patients_PatientId",
                table: "PatientConditions",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientConditions_Users_RecordedByUserId",
                table: "PatientConditions",
                column: "RecordedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedications_Patients_PatientId",
                table: "PatientMedications",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedications_Users_RecordedByUserId",
                table: "PatientMedications",
                column: "RecordedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfileFieldOptions_PatientProfileFields_FieldId",
                table: "PatientProfileFieldOptions",
                column: "FieldId",
                principalTable: "PatientProfileFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfileFields_PatientProfileSections_SectionId",
                table: "PatientProfileFields",
                column: "SectionId",
                principalTable: "PatientProfileSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfileValues_Files_FileId",
                table: "PatientProfileValues",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfileValues_PatientProfileFields_FieldId",
                table: "PatientProfileValues",
                column: "FieldId",
                principalTable: "PatientProfileFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfileValues_Patients_PatientId",
                table: "PatientProfileValues",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
