using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AuranClinicDbContext))]
[Migration("20261014210500_CreateSpecialtyDocumentationPolicies")]
public sealed class CreateSpecialtyDocumentationPolicies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE dbo.ClinicDocumentationSpecialtyPolicies (
                ClinicId uniqueidentifier NOT NULL,
                SpecialtyCode nvarchar(64) NOT NULL,
                RequiredFields nvarchar(256) NOT NULL,
                Version int NOT NULL CONSTRAINT DF_DocumentationPolicy_Version DEFAULT (1),
                UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_DocumentationPolicy_Updated DEFAULT SYSUTCDATETIME(),
                UpdatedByUserId uniqueidentifier NOT NULL,
                CONSTRAINT PK_ClinicDocumentationSpecialtyPolicies PRIMARY KEY (ClinicId, SpecialtyCode),
                CONSTRAINT CK_DocumentationPolicy_Version CHECK (Version > 0)
            );
            CREATE TABLE dbo.DoctorDocumentationSpecialties (
                ClinicId uniqueidentifier NOT NULL,
                DoctorId uniqueidentifier NOT NULL,
                SpecialtyCode nvarchar(64) NOT NULL,
                UpdatedAtUtc datetime2 NOT NULL CONSTRAINT DF_DoctorDocumentationSpecialty_Updated DEFAULT SYSUTCDATETIME(),
                CONSTRAINT PK_DoctorDocumentationSpecialties PRIMARY KEY (ClinicId, DoctorId)
            );
            CREATE INDEX IX_DoctorDocumentationSpecialties_Policy
                ON dbo.DoctorDocumentationSpecialties (ClinicId, SpecialtyCode);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE dbo.DoctorDocumentationSpecialties;
            DROP TABLE dbo.ClinicDocumentationSpecialtyPolicies;
            """);
    }
}
