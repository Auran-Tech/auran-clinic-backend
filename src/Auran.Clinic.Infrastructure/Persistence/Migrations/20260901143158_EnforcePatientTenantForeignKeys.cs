using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePatientTenantForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientProfileValues_FieldId",
                table: "PatientProfileValues");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileValues_FileId",
                table: "PatientProfileValues");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileValues_PatientId",
                table: "PatientProfileValues");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileFields_SectionId",
                table: "PatientProfileFields");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileFieldOptions_FieldId",
                table: "PatientProfileFieldOptions");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedications_PatientId",
                table: "PatientMedications");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedications_RecordedByUserId",
                table: "PatientMedications");

            migrationBuilder.DropIndex(
                name: "IX_PatientConditions_PatientId",
                table: "PatientConditions");

            migrationBuilder.DropIndex(
                name: "IX_PatientConditions_RecordedByUserId",
                table: "PatientConditions");

            migrationBuilder.DropIndex(
                name: "IX_PatientAttachments_FileId",
                table: "PatientAttachments");

            migrationBuilder.DropIndex(
                name: "IX_PatientAttachments_PatientId",
                table: "PatientAttachments");

            migrationBuilder.DropIndex(
                name: "IX_PatientAllergies_PatientId",
                table: "PatientAllergies");

            migrationBuilder.DropIndex(
                name: "IX_PatientAllergies_RecordedByUserId",
                table: "PatientAllergies");

            migrationBuilder.DropIndex(
                name: "IX_Files_UploadedByUserId",
                table: "Files");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Patients_Id_ClinicId",
                table: "Patients",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PatientProfileSections_Id_ClinicId",
                table: "PatientProfileSections",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PatientProfileFields_Id_ClinicId",
                table: "PatientProfileFields",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Files_Id_ClinicId",
                table: "Files",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValues_FieldId_ClinicId",
                table: "PatientProfileValues",
                columns: new[] { "FieldId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValues_FileId_ClinicId",
                table: "PatientProfileValues",
                columns: new[] { "FileId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValues_PatientId_ClinicId",
                table: "PatientProfileValues",
                columns: new[] { "PatientId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileFields_SectionId_ClinicId",
                table: "PatientProfileFields",
                columns: new[] { "SectionId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileFieldOptions_FieldId_ClinicId",
                table: "PatientProfileFieldOptions",
                columns: new[] { "FieldId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_PatientId_ClinicId",
                table: "PatientMedications",
                columns: new[] { "PatientId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_RecordedByUserId_ClinicId",
                table: "PatientMedications",
                columns: new[] { "RecordedByUserId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_PatientId_ClinicId",
                table: "PatientConditions",
                columns: new[] { "PatientId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_RecordedByUserId_ClinicId",
                table: "PatientConditions",
                columns: new[] { "RecordedByUserId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAttachments_FileId_ClinicId",
                table: "PatientAttachments",
                columns: new[] { "FileId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAttachments_PatientId_ClinicId",
                table: "PatientAttachments",
                columns: new[] { "PatientId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_PatientId_ClinicId",
                table: "PatientAllergies",
                columns: new[] { "PatientId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_RecordedByUserId_ClinicId",
                table: "PatientAllergies",
                columns: new[] { "RecordedByUserId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_UploadedByUserId_ClinicId",
                table: "Files",
                columns: new[] { "UploadedByUserId", "ClinicId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Users_UploadedByUserId_ClinicId",
                table: "Files",
                columns: new[] { "UploadedByUserId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAllergies_Patients_PatientId_ClinicId",
                table: "PatientAllergies",
                columns: new[] { "PatientId", "ClinicId" },
                principalTable: "Patients",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAllergies_Users_RecordedByUserId_ClinicId",
                table: "PatientAllergies",
                columns: new[] { "RecordedByUserId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAttachments_Files_FileId_ClinicId",
                table: "PatientAttachments",
                columns: new[] { "FileId", "ClinicId" },
                principalTable: "Files",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientAttachments_Patients_PatientId_ClinicId",
                table: "PatientAttachments",
                columns: new[] { "PatientId", "ClinicId" },
                principalTable: "Patients",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientConditions_Patients_PatientId_ClinicId",
                table: "PatientConditions",
                columns: new[] { "PatientId", "ClinicId" },
                principalTable: "Patients",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientConditions_Users_RecordedByUserId_ClinicId",
                table: "PatientConditions",
                columns: new[] { "RecordedByUserId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedications_Patients_PatientId_ClinicId",
                table: "PatientMedications",
                columns: new[] { "PatientId", "ClinicId" },
                principalTable: "Patients",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientMedications_Users_RecordedByUserId_ClinicId",
                table: "PatientMedications",
                columns: new[] { "RecordedByUserId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfileFieldOptions_PatientProfileFields_FieldId_ClinicId",
                table: "PatientProfileFieldOptions",
                columns: new[] { "FieldId", "ClinicId" },
                principalTable: "PatientProfileFields",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfileFields_PatientProfileSections_SectionId_ClinicId",
                table: "PatientProfileFields",
                columns: new[] { "SectionId", "ClinicId" },
                principalTable: "PatientProfileSections",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfileValues_Files_FileId_ClinicId",
                table: "PatientProfileValues",
                columns: new[] { "FileId", "ClinicId" },
                principalTable: "Files",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfileValues_PatientProfileFields_FieldId_ClinicId",
                table: "PatientProfileValues",
                columns: new[] { "FieldId", "ClinicId" },
                principalTable: "PatientProfileFields",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfileValues_Patients_PatientId_ClinicId",
                table: "PatientProfileValues",
                columns: new[] { "PatientId", "ClinicId" },
                principalTable: "Patients",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Users_UploadedByUserId_ClinicId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientAllergies_Patients_PatientId_ClinicId",
                table: "PatientAllergies");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientAllergies_Users_RecordedByUserId_ClinicId",
                table: "PatientAllergies");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientAttachments_Files_FileId_ClinicId",
                table: "PatientAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientAttachments_Patients_PatientId_ClinicId",
                table: "PatientAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientConditions_Patients_PatientId_ClinicId",
                table: "PatientConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientConditions_Users_RecordedByUserId_ClinicId",
                table: "PatientConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedications_Patients_PatientId_ClinicId",
                table: "PatientMedications");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientMedications_Users_RecordedByUserId_ClinicId",
                table: "PatientMedications");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfileFieldOptions_PatientProfileFields_FieldId_ClinicId",
                table: "PatientProfileFieldOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfileFields_PatientProfileSections_SectionId_ClinicId",
                table: "PatientProfileFields");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfileValues_Files_FileId_ClinicId",
                table: "PatientProfileValues");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfileValues_PatientProfileFields_FieldId_ClinicId",
                table: "PatientProfileValues");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfileValues_Patients_PatientId_ClinicId",
                table: "PatientProfileValues");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Patients_Id_ClinicId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileValues_FieldId_ClinicId",
                table: "PatientProfileValues");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileValues_FileId_ClinicId",
                table: "PatientProfileValues");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileValues_PatientId_ClinicId",
                table: "PatientProfileValues");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PatientProfileSections_Id_ClinicId",
                table: "PatientProfileSections");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PatientProfileFields_Id_ClinicId",
                table: "PatientProfileFields");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileFields_SectionId_ClinicId",
                table: "PatientProfileFields");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfileFieldOptions_FieldId_ClinicId",
                table: "PatientProfileFieldOptions");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedications_PatientId_ClinicId",
                table: "PatientMedications");

            migrationBuilder.DropIndex(
                name: "IX_PatientMedications_RecordedByUserId_ClinicId",
                table: "PatientMedications");

            migrationBuilder.DropIndex(
                name: "IX_PatientConditions_PatientId_ClinicId",
                table: "PatientConditions");

            migrationBuilder.DropIndex(
                name: "IX_PatientConditions_RecordedByUserId_ClinicId",
                table: "PatientConditions");

            migrationBuilder.DropIndex(
                name: "IX_PatientAttachments_FileId_ClinicId",
                table: "PatientAttachments");

            migrationBuilder.DropIndex(
                name: "IX_PatientAttachments_PatientId_ClinicId",
                table: "PatientAttachments");

            migrationBuilder.DropIndex(
                name: "IX_PatientAllergies_PatientId_ClinicId",
                table: "PatientAllergies");

            migrationBuilder.DropIndex(
                name: "IX_PatientAllergies_RecordedByUserId_ClinicId",
                table: "PatientAllergies");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Files_Id_ClinicId",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_UploadedByUserId_ClinicId",
                table: "Files");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValues_FieldId",
                table: "PatientProfileValues",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValues_FileId",
                table: "PatientProfileValues",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileValues_PatientId",
                table: "PatientProfileValues",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileFields_SectionId",
                table: "PatientProfileFields",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfileFieldOptions_FieldId",
                table: "PatientProfileFieldOptions",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_PatientId",
                table: "PatientMedications",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_RecordedByUserId",
                table: "PatientMedications",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_PatientId",
                table: "PatientConditions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_RecordedByUserId",
                table: "PatientConditions",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAttachments_FileId",
                table: "PatientAttachments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAttachments_PatientId",
                table: "PatientAttachments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_PatientId",
                table: "PatientAllergies",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_RecordedByUserId",
                table: "PatientAllergies",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_UploadedByUserId",
                table: "Files",
                column: "UploadedByUserId");
        }
    }
}
