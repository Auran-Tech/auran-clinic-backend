using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AuranClinicDbContext))]
[Migration("20260901143157_ValidatePatientTenantReferences")]
public sealed class ValidatePatientTenantReferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM [Files] AS f
                LEFT JOIN [Users] AS u
                    ON u.[Id] = f.[UploadedByUserId]
                    AND u.[ClinicId] = f.[ClinicId]
                WHERE u.[Id] IS NULL
            )
                THROW 51101, 'Cannot enforce file tenant ownership because cross-clinic uploader references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [PatientAllergies] AS a
                LEFT JOIN [Patients] AS p
                    ON p.[Id] = a.[PatientId]
                    AND p.[ClinicId] = a.[ClinicId]
                LEFT JOIN [Users] AS u
                    ON u.[Id] = a.[RecordedByUserId]
                    AND u.[ClinicId] = a.[ClinicId]
                WHERE p.[Id] IS NULL OR u.[Id] IS NULL
            )
                THROW 51102, 'Cannot enforce allergy tenant ownership because cross-clinic patient or recorder references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [PatientConditions] AS c
                LEFT JOIN [Patients] AS p
                    ON p.[Id] = c.[PatientId]
                    AND p.[ClinicId] = c.[ClinicId]
                LEFT JOIN [Users] AS u
                    ON u.[Id] = c.[RecordedByUserId]
                    AND u.[ClinicId] = c.[ClinicId]
                WHERE p.[Id] IS NULL OR u.[Id] IS NULL
            )
                THROW 51103, 'Cannot enforce condition tenant ownership because cross-clinic patient or recorder references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [PatientMedications] AS m
                LEFT JOIN [Patients] AS p
                    ON p.[Id] = m.[PatientId]
                    AND p.[ClinicId] = m.[ClinicId]
                LEFT JOIN [Users] AS u
                    ON u.[Id] = m.[RecordedByUserId]
                    AND u.[ClinicId] = m.[ClinicId]
                WHERE p.[Id] IS NULL OR u.[Id] IS NULL
            )
                THROW 51104, 'Cannot enforce medication tenant ownership because cross-clinic patient or recorder references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [PatientAttachments] AS a
                LEFT JOIN [Patients] AS p
                    ON p.[Id] = a.[PatientId]
                    AND p.[ClinicId] = a.[ClinicId]
                LEFT JOIN [Files] AS f
                    ON f.[Id] = a.[FileId]
                    AND f.[ClinicId] = a.[ClinicId]
                WHERE p.[Id] IS NULL OR f.[Id] IS NULL
            )
                THROW 51105, 'Cannot enforce patient-attachment tenant ownership because cross-clinic patient or file references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [PatientProfileFields] AS f
                LEFT JOIN [PatientProfileSections] AS s
                    ON s.[Id] = f.[SectionId]
                    AND s.[ClinicId] = f.[ClinicId]
                WHERE s.[Id] IS NULL
            )
                THROW 51106, 'Cannot enforce patient-profile field tenant ownership because cross-clinic section references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [PatientProfileFieldOptions] AS o
                LEFT JOIN [PatientProfileFields] AS f
                    ON f.[Id] = o.[FieldId]
                    AND f.[ClinicId] = o.[ClinicId]
                WHERE f.[Id] IS NULL
            )
                THROW 51107, 'Cannot enforce patient-profile option tenant ownership because cross-clinic field references exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [PatientProfileValues] AS v
                LEFT JOIN [Patients] AS p
                    ON p.[Id] = v.[PatientId]
                    AND p.[ClinicId] = v.[ClinicId]
                LEFT JOIN [PatientProfileFields] AS pf
                    ON pf.[Id] = v.[FieldId]
                    AND pf.[ClinicId] = v.[ClinicId]
                LEFT JOIN [Files] AS f
                    ON f.[Id] = v.[FileId]
                    AND f.[ClinicId] = v.[ClinicId]
                WHERE p.[Id] IS NULL
                    OR pf.[Id] IS NULL
                    OR (v.[FileId] IS NOT NULL AND f.[Id] IS NULL)
            )
                THROW 51108, 'Cannot enforce patient-profile value tenant ownership because cross-clinic patient, field, or file references exist.', 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Validation only; no schema or data changes to revert.
    }
}
