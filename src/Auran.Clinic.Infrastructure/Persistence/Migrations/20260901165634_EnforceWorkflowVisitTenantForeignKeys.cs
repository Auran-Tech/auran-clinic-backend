using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceWorkflowVisitTenantForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalFieldOptions_ClinicalFields_ClinicalFieldId",
                table: "ClinicalFieldOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalMeasurements_ClinicalFields_ClinicalFieldId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalMeasurements_Patients_PatientId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalMeasurements_Users_RecordedByUserId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalMeasurements_Visits_VisitId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderAttachments_ClinicalOrderSections_ClinicalOrderSectionId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderAttachments_ClinicalOrders_ClinicalOrderId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderAttachments_Files_FileId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderItems_ClinicalOrderSections_ClinicalOrderSectionId",
                table: "ClinicalOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrders_Patients_PatientId",
                table: "ClinicalOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrders_Users_CreatedByUserId",
                table: "ClinicalOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrders_Users_DoctorId",
                table: "ClinicalOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrders_Visits_VisitId",
                table: "ClinicalOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderSections_ClinicalOrderSectionDefinitions_SectionDefinitionId",
                table: "ClinicalOrderSections");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderSections_ClinicalOrders_ClinicalOrderId",
                table: "ClinicalOrderSections");

            migrationBuilder.DropForeignKey(
                name: "FK_FollowUps_Patients_PatientId",
                table: "FollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_FollowUps_Users_DoctorId",
                table: "FollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_FollowUps_Visits_VisitId",
                table: "FollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Patients_PatientId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Users_DoctorId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Visits_VisitId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_WorkflowStatuses_WorkflowStatusId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueStatusHistory_QueueEntries_QueueEntryId",
                table: "QueueStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueStatusHistory_Users_ChangedByUserId",
                table: "QueueStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueStatusHistory_WorkflowStatuses_FromStatusId",
                table: "QueueStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueStatusHistory_WorkflowStatuses_ToStatusId",
                table: "QueueStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Patients_PatientId",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Users_DoctorId",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitSessions_Users_CreatedByUserId",
                table: "VisitSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitSessions_Users_DoctorId",
                table: "VisitSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitSessions_Visits_VisitId",
                table: "VisitSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTransitions_WorkflowStatuses_FromStatusId",
                table: "WorkflowTransitions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTransitions_WorkflowStatuses_ToStatusId",
                table: "WorkflowTransitions");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTransitions_FromStatusId",
                table: "WorkflowTransitions");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTransitions_ToStatusId",
                table: "WorkflowTransitions");

            migrationBuilder.DropIndex(
                name: "IX_VisitSessions_CreatedByUserId",
                table: "VisitSessions");

            migrationBuilder.DropIndex(
                name: "IX_VisitSessions_DoctorId",
                table: "VisitSessions");

            migrationBuilder.DropIndex(
                name: "IX_VisitSessions_VisitId",
                table: "VisitSessions");

            migrationBuilder.DropIndex(
                name: "IX_Visits_DoctorId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_PatientId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_QueueStatusHistory_ChangedByUserId",
                table: "QueueStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_QueueStatusHistory_FromStatusId",
                table: "QueueStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_QueueStatusHistory_QueueEntryId",
                table: "QueueStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_QueueStatusHistory_ToStatusId",
                table: "QueueStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_DoctorId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_PatientId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_VisitId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_WorkflowStatusId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_DoctorId",
                table: "FollowUps");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_PatientId",
                table: "FollowUps");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_VisitId",
                table: "FollowUps");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderSections_ClinicalOrderId",
                table: "ClinicalOrderSections");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderSections_SectionDefinitionId",
                table: "ClinicalOrderSections");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrders_CreatedByUserId",
                table: "ClinicalOrders");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrders_DoctorId",
                table: "ClinicalOrders");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrders_PatientId",
                table: "ClinicalOrders");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrders_VisitId",
                table: "ClinicalOrders");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderItems_ClinicalOrderSectionId",
                table: "ClinicalOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderAttachments_ClinicalOrderId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderAttachments_ClinicalOrderSectionId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderAttachments_FileId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalMeasurements_ClinicalFieldId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalMeasurements_PatientId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalMeasurements_RecordedByUserId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalMeasurements_VisitId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalFieldOptions_ClinicalFieldId",
                table: "ClinicalFieldOptions");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_WorkflowStatuses_Id_ClinicId",
                table: "WorkflowStatuses",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Visits_Id_ClinicId",
                table: "Visits",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_QueueEntries_Id_ClinicId",
                table: "QueueEntries",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ClinicalOrderSections_Id_ClinicId",
                table: "ClinicalOrderSections",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ClinicalOrderSectionDefinitions_Id_ClinicId",
                table: "ClinicalOrderSectionDefinitions",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ClinicalOrders_Id_ClinicId",
                table: "ClinicalOrders",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ClinicalFields_Id_ClinicId",
                table: "ClinicalFields",
                columns: new[] { "Id", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_FromStatusId_ClinicId",
                table: "WorkflowTransitions",
                columns: new[] { "FromStatusId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_ToStatusId_ClinicId",
                table: "WorkflowTransitions",
                columns: new[] { "ToStatusId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_CreatedByUserId_ClinicId",
                table: "VisitSessions",
                columns: new[] { "CreatedByUserId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_DoctorId_ClinicId",
                table: "VisitSessions",
                columns: new[] { "DoctorId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_VisitId_ClinicId",
                table: "VisitSessions",
                columns: new[] { "VisitId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_DoctorId_ClinicId",
                table: "Visits",
                columns: new[] { "DoctorId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PatientId_ClinicId",
                table: "Visits",
                columns: new[] { "PatientId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatusHistory_ChangedByUserId_ClinicId",
                table: "QueueStatusHistory",
                columns: new[] { "ChangedByUserId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatusHistory_FromStatusId_ClinicId",
                table: "QueueStatusHistory",
                columns: new[] { "FromStatusId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatusHistory_QueueEntryId_ClinicId",
                table: "QueueStatusHistory",
                columns: new[] { "QueueEntryId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatusHistory_ToStatusId_ClinicId",
                table: "QueueStatusHistory",
                columns: new[] { "ToStatusId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_DoctorId_ClinicId",
                table: "QueueEntries",
                columns: new[] { "DoctorId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_PatientId_ClinicId",
                table: "QueueEntries",
                columns: new[] { "PatientId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_VisitId_ClinicId",
                table: "QueueEntries",
                columns: new[] { "VisitId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_WorkflowStatusId_ClinicId",
                table: "QueueEntries",
                columns: new[] { "WorkflowStatusId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_DoctorId_ClinicId",
                table: "FollowUps",
                columns: new[] { "DoctorId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_PatientId_ClinicId",
                table: "FollowUps",
                columns: new[] { "PatientId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_VisitId_ClinicId",
                table: "FollowUps",
                columns: new[] { "VisitId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderSections_ClinicalOrderId_ClinicId",
                table: "ClinicalOrderSections",
                columns: new[] { "ClinicalOrderId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderSections_SectionDefinitionId_ClinicId",
                table: "ClinicalOrderSections",
                columns: new[] { "SectionDefinitionId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrders_CreatedByUserId_ClinicId",
                table: "ClinicalOrders",
                columns: new[] { "CreatedByUserId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrders_DoctorId_ClinicId",
                table: "ClinicalOrders",
                columns: new[] { "DoctorId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrders_PatientId_ClinicId",
                table: "ClinicalOrders",
                columns: new[] { "PatientId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrders_VisitId_ClinicId",
                table: "ClinicalOrders",
                columns: new[] { "VisitId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderItems_ClinicalOrderSectionId_ClinicId",
                table: "ClinicalOrderItems",
                columns: new[] { "ClinicalOrderSectionId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderAttachments_ClinicalOrderId_ClinicId",
                table: "ClinicalOrderAttachments",
                columns: new[] { "ClinicalOrderId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderAttachments_ClinicalOrderSectionId_ClinicId",
                table: "ClinicalOrderAttachments",
                columns: new[] { "ClinicalOrderSectionId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderAttachments_FileId_ClinicId",
                table: "ClinicalOrderAttachments",
                columns: new[] { "FileId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalMeasurements_ClinicalFieldId_ClinicId",
                table: "ClinicalMeasurements",
                columns: new[] { "ClinicalFieldId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalMeasurements_PatientId_ClinicId",
                table: "ClinicalMeasurements",
                columns: new[] { "PatientId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalMeasurements_RecordedByUserId_ClinicId",
                table: "ClinicalMeasurements",
                columns: new[] { "RecordedByUserId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalMeasurements_VisitId_ClinicId",
                table: "ClinicalMeasurements",
                columns: new[] { "VisitId", "ClinicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalFieldOptions_ClinicalFieldId_ClinicId",
                table: "ClinicalFieldOptions",
                columns: new[] { "ClinicalFieldId", "ClinicId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalFieldOptions_ClinicalFields_ClinicalFieldId_ClinicId",
                table: "ClinicalFieldOptions",
                columns: new[] { "ClinicalFieldId", "ClinicId" },
                principalTable: "ClinicalFields",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalMeasurements_ClinicalFields_ClinicalFieldId_ClinicId",
                table: "ClinicalMeasurements",
                columns: new[] { "ClinicalFieldId", "ClinicId" },
                principalTable: "ClinicalFields",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalMeasurements_Patients_PatientId_ClinicId",
                table: "ClinicalMeasurements",
                columns: new[] { "PatientId", "ClinicId" },
                principalTable: "Patients",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalMeasurements_Users_RecordedByUserId_ClinicId",
                table: "ClinicalMeasurements",
                columns: new[] { "RecordedByUserId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalMeasurements_Visits_VisitId_ClinicId",
                table: "ClinicalMeasurements",
                columns: new[] { "VisitId", "ClinicId" },
                principalTable: "Visits",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderAttachments_ClinicalOrderSections_ClinicalOrderSectionId_ClinicId",
                table: "ClinicalOrderAttachments",
                columns: new[] { "ClinicalOrderSectionId", "ClinicId" },
                principalTable: "ClinicalOrderSections",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderAttachments_ClinicalOrders_ClinicalOrderId_ClinicId",
                table: "ClinicalOrderAttachments",
                columns: new[] { "ClinicalOrderId", "ClinicId" },
                principalTable: "ClinicalOrders",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderAttachments_Files_FileId_ClinicId",
                table: "ClinicalOrderAttachments",
                columns: new[] { "FileId", "ClinicId" },
                principalTable: "Files",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderItems_ClinicalOrderSections_ClinicalOrderSectionId_ClinicId",
                table: "ClinicalOrderItems",
                columns: new[] { "ClinicalOrderSectionId", "ClinicId" },
                principalTable: "ClinicalOrderSections",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrders_Patients_PatientId_ClinicId",
                table: "ClinicalOrders",
                columns: new[] { "PatientId", "ClinicId" },
                principalTable: "Patients",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrders_Users_CreatedByUserId_ClinicId",
                table: "ClinicalOrders",
                columns: new[] { "CreatedByUserId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrders_Users_DoctorId_ClinicId",
                table: "ClinicalOrders",
                columns: new[] { "DoctorId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrders_Visits_VisitId_ClinicId",
                table: "ClinicalOrders",
                columns: new[] { "VisitId", "ClinicId" },
                principalTable: "Visits",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderSections_ClinicalOrderSectionDefinitions_SectionDefinitionId_ClinicId",
                table: "ClinicalOrderSections",
                columns: new[] { "SectionDefinitionId", "ClinicId" },
                principalTable: "ClinicalOrderSectionDefinitions",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderSections_ClinicalOrders_ClinicalOrderId_ClinicId",
                table: "ClinicalOrderSections",
                columns: new[] { "ClinicalOrderId", "ClinicId" },
                principalTable: "ClinicalOrders",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FollowUps_Patients_PatientId_ClinicId",
                table: "FollowUps",
                columns: new[] { "PatientId", "ClinicId" },
                principalTable: "Patients",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FollowUps_Users_DoctorId_ClinicId",
                table: "FollowUps",
                columns: new[] { "DoctorId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FollowUps_Visits_VisitId_ClinicId",
                table: "FollowUps",
                columns: new[] { "VisitId", "ClinicId" },
                principalTable: "Visits",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Patients_PatientId_ClinicId",
                table: "QueueEntries",
                columns: new[] { "PatientId", "ClinicId" },
                principalTable: "Patients",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Users_DoctorId_ClinicId",
                table: "QueueEntries",
                columns: new[] { "DoctorId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Visits_VisitId_ClinicId",
                table: "QueueEntries",
                columns: new[] { "VisitId", "ClinicId" },
                principalTable: "Visits",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_WorkflowStatuses_WorkflowStatusId_ClinicId",
                table: "QueueEntries",
                columns: new[] { "WorkflowStatusId", "ClinicId" },
                principalTable: "WorkflowStatuses",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueStatusHistory_QueueEntries_QueueEntryId_ClinicId",
                table: "QueueStatusHistory",
                columns: new[] { "QueueEntryId", "ClinicId" },
                principalTable: "QueueEntries",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueStatusHistory_Users_ChangedByUserId_ClinicId",
                table: "QueueStatusHistory",
                columns: new[] { "ChangedByUserId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueStatusHistory_WorkflowStatuses_FromStatusId_ClinicId",
                table: "QueueStatusHistory",
                columns: new[] { "FromStatusId", "ClinicId" },
                principalTable: "WorkflowStatuses",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueStatusHistory_WorkflowStatuses_ToStatusId_ClinicId",
                table: "QueueStatusHistory",
                columns: new[] { "ToStatusId", "ClinicId" },
                principalTable: "WorkflowStatuses",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Patients_PatientId_ClinicId",
                table: "Visits",
                columns: new[] { "PatientId", "ClinicId" },
                principalTable: "Patients",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Users_DoctorId_ClinicId",
                table: "Visits",
                columns: new[] { "DoctorId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VisitSessions_Users_CreatedByUserId_ClinicId",
                table: "VisitSessions",
                columns: new[] { "CreatedByUserId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VisitSessions_Users_DoctorId_ClinicId",
                table: "VisitSessions",
                columns: new[] { "DoctorId", "ClinicId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VisitSessions_Visits_VisitId_ClinicId",
                table: "VisitSessions",
                columns: new[] { "VisitId", "ClinicId" },
                principalTable: "Visits",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTransitions_WorkflowStatuses_FromStatusId_ClinicId",
                table: "WorkflowTransitions",
                columns: new[] { "FromStatusId", "ClinicId" },
                principalTable: "WorkflowStatuses",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTransitions_WorkflowStatuses_ToStatusId_ClinicId",
                table: "WorkflowTransitions",
                columns: new[] { "ToStatusId", "ClinicId" },
                principalTable: "WorkflowStatuses",
                principalColumns: new[] { "Id", "ClinicId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalFieldOptions_ClinicalFields_ClinicalFieldId_ClinicId",
                table: "ClinicalFieldOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalMeasurements_ClinicalFields_ClinicalFieldId_ClinicId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalMeasurements_Patients_PatientId_ClinicId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalMeasurements_Users_RecordedByUserId_ClinicId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalMeasurements_Visits_VisitId_ClinicId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderAttachments_ClinicalOrderSections_ClinicalOrderSectionId_ClinicId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderAttachments_ClinicalOrders_ClinicalOrderId_ClinicId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderAttachments_Files_FileId_ClinicId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderItems_ClinicalOrderSections_ClinicalOrderSectionId_ClinicId",
                table: "ClinicalOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrders_Patients_PatientId_ClinicId",
                table: "ClinicalOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrders_Users_CreatedByUserId_ClinicId",
                table: "ClinicalOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrders_Users_DoctorId_ClinicId",
                table: "ClinicalOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrders_Visits_VisitId_ClinicId",
                table: "ClinicalOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderSections_ClinicalOrderSectionDefinitions_SectionDefinitionId_ClinicId",
                table: "ClinicalOrderSections");

            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalOrderSections_ClinicalOrders_ClinicalOrderId_ClinicId",
                table: "ClinicalOrderSections");

            migrationBuilder.DropForeignKey(
                name: "FK_FollowUps_Patients_PatientId_ClinicId",
                table: "FollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_FollowUps_Users_DoctorId_ClinicId",
                table: "FollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_FollowUps_Visits_VisitId_ClinicId",
                table: "FollowUps");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Patients_PatientId_ClinicId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Users_DoctorId_ClinicId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_Visits_VisitId_ClinicId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueEntries_WorkflowStatuses_WorkflowStatusId_ClinicId",
                table: "QueueEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueStatusHistory_QueueEntries_QueueEntryId_ClinicId",
                table: "QueueStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueStatusHistory_Users_ChangedByUserId_ClinicId",
                table: "QueueStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueStatusHistory_WorkflowStatuses_FromStatusId_ClinicId",
                table: "QueueStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_QueueStatusHistory_WorkflowStatuses_ToStatusId_ClinicId",
                table: "QueueStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Patients_PatientId_ClinicId",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_Users_DoctorId_ClinicId",
                table: "Visits");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitSessions_Users_CreatedByUserId_ClinicId",
                table: "VisitSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitSessions_Users_DoctorId_ClinicId",
                table: "VisitSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitSessions_Visits_VisitId_ClinicId",
                table: "VisitSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTransitions_WorkflowStatuses_FromStatusId_ClinicId",
                table: "WorkflowTransitions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTransitions_WorkflowStatuses_ToStatusId_ClinicId",
                table: "WorkflowTransitions");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTransitions_FromStatusId_ClinicId",
                table: "WorkflowTransitions");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTransitions_ToStatusId_ClinicId",
                table: "WorkflowTransitions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_WorkflowStatuses_Id_ClinicId",
                table: "WorkflowStatuses");

            migrationBuilder.DropIndex(
                name: "IX_VisitSessions_CreatedByUserId_ClinicId",
                table: "VisitSessions");

            migrationBuilder.DropIndex(
                name: "IX_VisitSessions_DoctorId_ClinicId",
                table: "VisitSessions");

            migrationBuilder.DropIndex(
                name: "IX_VisitSessions_VisitId_ClinicId",
                table: "VisitSessions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Visits_Id_ClinicId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_DoctorId_ClinicId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_PatientId_ClinicId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_QueueStatusHistory_ChangedByUserId_ClinicId",
                table: "QueueStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_QueueStatusHistory_FromStatusId_ClinicId",
                table: "QueueStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_QueueStatusHistory_QueueEntryId_ClinicId",
                table: "QueueStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_QueueStatusHistory_ToStatusId_ClinicId",
                table: "QueueStatusHistory");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_QueueEntries_Id_ClinicId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_DoctorId_ClinicId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_PatientId_ClinicId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_VisitId_ClinicId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_WorkflowStatusId_ClinicId",
                table: "QueueEntries");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_DoctorId_ClinicId",
                table: "FollowUps");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_PatientId_ClinicId",
                table: "FollowUps");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_VisitId_ClinicId",
                table: "FollowUps");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ClinicalOrderSections_Id_ClinicId",
                table: "ClinicalOrderSections");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderSections_ClinicalOrderId_ClinicId",
                table: "ClinicalOrderSections");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderSections_SectionDefinitionId_ClinicId",
                table: "ClinicalOrderSections");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ClinicalOrderSectionDefinitions_Id_ClinicId",
                table: "ClinicalOrderSectionDefinitions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ClinicalOrders_Id_ClinicId",
                table: "ClinicalOrders");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrders_CreatedByUserId_ClinicId",
                table: "ClinicalOrders");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrders_DoctorId_ClinicId",
                table: "ClinicalOrders");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrders_PatientId_ClinicId",
                table: "ClinicalOrders");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrders_VisitId_ClinicId",
                table: "ClinicalOrders");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderItems_ClinicalOrderSectionId_ClinicId",
                table: "ClinicalOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderAttachments_ClinicalOrderId_ClinicId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderAttachments_ClinicalOrderSectionId_ClinicId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderAttachments_FileId_ClinicId",
                table: "ClinicalOrderAttachments");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalMeasurements_ClinicalFieldId_ClinicId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalMeasurements_PatientId_ClinicId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalMeasurements_RecordedByUserId_ClinicId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalMeasurements_VisitId_ClinicId",
                table: "ClinicalMeasurements");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ClinicalFields_Id_ClinicId",
                table: "ClinicalFields");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalFieldOptions_ClinicalFieldId_ClinicId",
                table: "ClinicalFieldOptions");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_FromStatusId",
                table: "WorkflowTransitions",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_ToStatusId",
                table: "WorkflowTransitions",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_CreatedByUserId",
                table: "VisitSessions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_DoctorId",
                table: "VisitSessions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_VisitId",
                table: "VisitSessions",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_DoctorId",
                table: "Visits",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PatientId",
                table: "Visits",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatusHistory_ChangedByUserId",
                table: "QueueStatusHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatusHistory_FromStatusId",
                table: "QueueStatusHistory",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatusHistory_QueueEntryId",
                table: "QueueStatusHistory",
                column: "QueueEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueStatusHistory_ToStatusId",
                table: "QueueStatusHistory",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_DoctorId",
                table: "QueueEntries",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_PatientId",
                table: "QueueEntries",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_VisitId",
                table: "QueueEntries",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_WorkflowStatusId",
                table: "QueueEntries",
                column: "WorkflowStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_DoctorId",
                table: "FollowUps",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_PatientId",
                table: "FollowUps",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_VisitId",
                table: "FollowUps",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderSections_ClinicalOrderId",
                table: "ClinicalOrderSections",
                column: "ClinicalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderSections_SectionDefinitionId",
                table: "ClinicalOrderSections",
                column: "SectionDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrders_CreatedByUserId",
                table: "ClinicalOrders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrders_DoctorId",
                table: "ClinicalOrders",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrders_PatientId",
                table: "ClinicalOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrders_VisitId",
                table: "ClinicalOrders",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderItems_ClinicalOrderSectionId",
                table: "ClinicalOrderItems",
                column: "ClinicalOrderSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderAttachments_ClinicalOrderId",
                table: "ClinicalOrderAttachments",
                column: "ClinicalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderAttachments_ClinicalOrderSectionId",
                table: "ClinicalOrderAttachments",
                column: "ClinicalOrderSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderAttachments_FileId",
                table: "ClinicalOrderAttachments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalMeasurements_ClinicalFieldId",
                table: "ClinicalMeasurements",
                column: "ClinicalFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalMeasurements_PatientId",
                table: "ClinicalMeasurements",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalMeasurements_RecordedByUserId",
                table: "ClinicalMeasurements",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalMeasurements_VisitId",
                table: "ClinicalMeasurements",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalFieldOptions_ClinicalFieldId",
                table: "ClinicalFieldOptions",
                column: "ClinicalFieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalFieldOptions_ClinicalFields_ClinicalFieldId",
                table: "ClinicalFieldOptions",
                column: "ClinicalFieldId",
                principalTable: "ClinicalFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalMeasurements_ClinicalFields_ClinicalFieldId",
                table: "ClinicalMeasurements",
                column: "ClinicalFieldId",
                principalTable: "ClinicalFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalMeasurements_Patients_PatientId",
                table: "ClinicalMeasurements",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalMeasurements_Users_RecordedByUserId",
                table: "ClinicalMeasurements",
                column: "RecordedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalMeasurements_Visits_VisitId",
                table: "ClinicalMeasurements",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderAttachments_ClinicalOrderSections_ClinicalOrderSectionId",
                table: "ClinicalOrderAttachments",
                column: "ClinicalOrderSectionId",
                principalTable: "ClinicalOrderSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderAttachments_ClinicalOrders_ClinicalOrderId",
                table: "ClinicalOrderAttachments",
                column: "ClinicalOrderId",
                principalTable: "ClinicalOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderAttachments_Files_FileId",
                table: "ClinicalOrderAttachments",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderItems_ClinicalOrderSections_ClinicalOrderSectionId",
                table: "ClinicalOrderItems",
                column: "ClinicalOrderSectionId",
                principalTable: "ClinicalOrderSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrders_Patients_PatientId",
                table: "ClinicalOrders",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrders_Users_CreatedByUserId",
                table: "ClinicalOrders",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrders_Users_DoctorId",
                table: "ClinicalOrders",
                column: "DoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrders_Visits_VisitId",
                table: "ClinicalOrders",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderSections_ClinicalOrderSectionDefinitions_SectionDefinitionId",
                table: "ClinicalOrderSections",
                column: "SectionDefinitionId",
                principalTable: "ClinicalOrderSectionDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalOrderSections_ClinicalOrders_ClinicalOrderId",
                table: "ClinicalOrderSections",
                column: "ClinicalOrderId",
                principalTable: "ClinicalOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FollowUps_Patients_PatientId",
                table: "FollowUps",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FollowUps_Users_DoctorId",
                table: "FollowUps",
                column: "DoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FollowUps_Visits_VisitId",
                table: "FollowUps",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Patients_PatientId",
                table: "QueueEntries",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Users_DoctorId",
                table: "QueueEntries",
                column: "DoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_Visits_VisitId",
                table: "QueueEntries",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueEntries_WorkflowStatuses_WorkflowStatusId",
                table: "QueueEntries",
                column: "WorkflowStatusId",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueStatusHistory_QueueEntries_QueueEntryId",
                table: "QueueStatusHistory",
                column: "QueueEntryId",
                principalTable: "QueueEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueStatusHistory_Users_ChangedByUserId",
                table: "QueueStatusHistory",
                column: "ChangedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueStatusHistory_WorkflowStatuses_FromStatusId",
                table: "QueueStatusHistory",
                column: "FromStatusId",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QueueStatusHistory_WorkflowStatuses_ToStatusId",
                table: "QueueStatusHistory",
                column: "ToStatusId",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Patients_PatientId",
                table: "Visits",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_Users_DoctorId",
                table: "Visits",
                column: "DoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VisitSessions_Users_CreatedByUserId",
                table: "VisitSessions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VisitSessions_Users_DoctorId",
                table: "VisitSessions",
                column: "DoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VisitSessions_Visits_VisitId",
                table: "VisitSessions",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTransitions_WorkflowStatuses_FromStatusId",
                table: "WorkflowTransitions",
                column: "FromStatusId",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTransitions_WorkflowStatuses_ToStatusId",
                table: "WorkflowTransitions",
                column: "ToStatusId",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
