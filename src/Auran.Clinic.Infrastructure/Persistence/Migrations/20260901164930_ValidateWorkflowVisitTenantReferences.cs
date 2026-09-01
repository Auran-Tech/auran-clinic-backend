using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AuranClinicDbContext))]
[Migration("20260901164930_ValidateWorkflowVisitTenantReferences")]
public sealed class ValidateWorkflowVisitTenantReferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM [WorkflowTransitions] AS t
                LEFT JOIN [WorkflowStatuses] AS fs
                    ON fs.[Id] = t.[FromStatusId] AND fs.[ClinicId] = t.[ClinicId]
                LEFT JOIN [WorkflowStatuses] AS ts
                    ON ts.[Id] = t.[ToStatusId] AND ts.[ClinicId] = t.[ClinicId]
                WHERE fs.[Id] IS NULL OR ts.[Id] IS NULL
            )
                THROW 51201, 'Cannot enforce workflow-transition tenant ownership because cross-clinic status references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [Visits] AS v
                LEFT JOIN [Patients] AS p
                    ON p.[Id] = v.[PatientId] AND p.[ClinicId] = v.[ClinicId]
                LEFT JOIN [Users] AS d
                    ON d.[Id] = v.[DoctorId] AND d.[ClinicId] = v.[ClinicId]
                WHERE p.[Id] IS NULL OR d.[Id] IS NULL
            )
                THROW 51202, 'Cannot enforce visit tenant ownership because cross-clinic patient or doctor references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [QueueEntries] AS q
                LEFT JOIN [Patients] AS p
                    ON p.[Id] = q.[PatientId] AND p.[ClinicId] = q.[ClinicId]
                LEFT JOIN [Visits] AS v
                    ON v.[Id] = q.[VisitId] AND v.[ClinicId] = q.[ClinicId]
                LEFT JOIN [Users] AS d
                    ON d.[Id] = q.[DoctorId] AND d.[ClinicId] = q.[ClinicId]
                LEFT JOIN [WorkflowStatuses] AS s
                    ON s.[Id] = q.[WorkflowStatusId] AND s.[ClinicId] = q.[ClinicId]
                WHERE p.[Id] IS NULL
                    OR v.[Id] IS NULL
                    OR (q.[DoctorId] IS NOT NULL AND d.[Id] IS NULL)
                    OR s.[Id] IS NULL
            )
                THROW 51203, 'Cannot enforce queue-entry tenant ownership because cross-clinic patient, visit, doctor, or status references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [QueueStatusHistory] AS h
                LEFT JOIN [QueueEntries] AS q
                    ON q.[Id] = h.[QueueEntryId] AND q.[ClinicId] = h.[ClinicId]
                LEFT JOIN [WorkflowStatuses] AS fs
                    ON fs.[Id] = h.[FromStatusId] AND fs.[ClinicId] = h.[ClinicId]
                LEFT JOIN [WorkflowStatuses] AS ts
                    ON ts.[Id] = h.[ToStatusId] AND ts.[ClinicId] = h.[ClinicId]
                LEFT JOIN [Users] AS u
                    ON u.[Id] = h.[ChangedByUserId] AND u.[ClinicId] = h.[ClinicId]
                WHERE q.[Id] IS NULL
                    OR (h.[FromStatusId] IS NOT NULL AND fs.[Id] IS NULL)
                    OR ts.[Id] IS NULL
                    OR u.[Id] IS NULL
            )
                THROW 51204, 'Cannot enforce queue-history tenant ownership because cross-clinic queue, status, or user references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [VisitSessions] AS s
                LEFT JOIN [Visits] AS v
                    ON v.[Id] = s.[VisitId] AND v.[ClinicId] = s.[ClinicId]
                LEFT JOIN [Users] AS d
                    ON d.[Id] = s.[DoctorId] AND d.[ClinicId] = s.[ClinicId]
                LEFT JOIN [Users] AS c
                    ON c.[Id] = s.[CreatedByUserId] AND c.[ClinicId] = s.[ClinicId]
                WHERE v.[Id] IS NULL OR d.[Id] IS NULL OR c.[Id] IS NULL
            )
                THROW 51205, 'Cannot enforce visit-session tenant ownership because cross-clinic visit or user references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [FollowUps] AS f
                LEFT JOIN [Patients] AS p
                    ON p.[Id] = f.[PatientId] AND p.[ClinicId] = f.[ClinicId]
                LEFT JOIN [Visits] AS v
                    ON v.[Id] = f.[VisitId] AND v.[ClinicId] = f.[ClinicId]
                LEFT JOIN [Users] AS d
                    ON d.[Id] = f.[DoctorId] AND d.[ClinicId] = f.[ClinicId]
                WHERE p.[Id] IS NULL OR v.[Id] IS NULL OR d.[Id] IS NULL
            )
                THROW 51206, 'Cannot enforce follow-up tenant ownership because cross-clinic patient, visit, or doctor references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [ClinicalFieldOptions] AS o
                LEFT JOIN [ClinicalFields] AS f
                    ON f.[Id] = o.[ClinicalFieldId] AND f.[ClinicId] = o.[ClinicId]
                WHERE f.[Id] IS NULL
            )
                THROW 51207, 'Cannot enforce clinical-field option tenant ownership because cross-clinic field references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [ClinicalMeasurements] AS m
                LEFT JOIN [Patients] AS p
                    ON p.[Id] = m.[PatientId] AND p.[ClinicId] = m.[ClinicId]
                LEFT JOIN [Visits] AS v
                    ON v.[Id] = m.[VisitId] AND v.[ClinicId] = m.[ClinicId]
                LEFT JOIN [ClinicalFields] AS f
                    ON f.[Id] = m.[ClinicalFieldId] AND f.[ClinicId] = m.[ClinicId]
                LEFT JOIN [Users] AS u
                    ON u.[Id] = m.[RecordedByUserId] AND u.[ClinicId] = m.[ClinicId]
                WHERE p.[Id] IS NULL
                    OR (m.[VisitId] IS NOT NULL AND v.[Id] IS NULL)
                    OR f.[Id] IS NULL
                    OR u.[Id] IS NULL
            )
                THROW 51208, 'Cannot enforce clinical-measurement tenant ownership because cross-clinic patient, visit, field, or recorder references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [ClinicalOrders] AS o
                LEFT JOIN [Visits] AS v
                    ON v.[Id] = o.[VisitId] AND v.[ClinicId] = o.[ClinicId]
                LEFT JOIN [Patients] AS p
                    ON p.[Id] = o.[PatientId] AND p.[ClinicId] = o.[ClinicId]
                LEFT JOIN [Users] AS d
                    ON d.[Id] = o.[DoctorId] AND d.[ClinicId] = o.[ClinicId]
                LEFT JOIN [Users] AS c
                    ON c.[Id] = o.[CreatedByUserId] AND c.[ClinicId] = o.[ClinicId]
                WHERE v.[Id] IS NULL OR p.[Id] IS NULL OR d.[Id] IS NULL OR c.[Id] IS NULL
            )
                THROW 51209, 'Cannot enforce clinical-order tenant ownership because cross-clinic visit, patient, or user references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [ClinicalOrderSections] AS s
                LEFT JOIN [ClinicalOrders] AS o
                    ON o.[Id] = s.[ClinicalOrderId] AND o.[ClinicId] = s.[ClinicId]
                LEFT JOIN [ClinicalOrderSectionDefinitions] AS d
                    ON d.[Id] = s.[SectionDefinitionId] AND d.[ClinicId] = s.[ClinicId]
                WHERE o.[Id] IS NULL OR d.[Id] IS NULL
            )
                THROW 51210, 'Cannot enforce clinical-order section tenant ownership because cross-clinic order or definition references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [ClinicalOrderItems] AS i
                LEFT JOIN [ClinicalOrderSections] AS s
                    ON s.[Id] = i.[ClinicalOrderSectionId] AND s.[ClinicId] = i.[ClinicId]
                WHERE s.[Id] IS NULL
            )
                THROW 51211, 'Cannot enforce clinical-order item tenant ownership because cross-clinic section references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [ClinicalOrderAttachments] AS a
                LEFT JOIN [ClinicalOrders] AS o
                    ON o.[Id] = a.[ClinicalOrderId] AND o.[ClinicId] = a.[ClinicId]
                LEFT JOIN [ClinicalOrderSections] AS s
                    ON s.[Id] = a.[ClinicalOrderSectionId] AND s.[ClinicId] = a.[ClinicId]
                LEFT JOIN [Files] AS f
                    ON f.[Id] = a.[FileId] AND f.[ClinicId] = a.[ClinicId]
                WHERE o.[Id] IS NULL
                    OR (a.[ClinicalOrderSectionId] IS NOT NULL AND s.[Id] IS NULL)
                    OR f.[Id] IS NULL
            )
                THROW 51212, 'Cannot enforce clinical-order attachment tenant ownership because cross-clinic order, section, or file references exist.', 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Validation only; no schema or data changes to revert.
    }
}
