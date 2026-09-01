using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AuranClinicDbContext))]
[Migration("20260901170521_ValidateAuditTenantReferences")]
public sealed class ValidateAuditTenantReferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM [AuditLogs] AS a
                LEFT JOIN [Users] AS u
                    ON u.[Id] = a.[ActorUserId]
                    AND u.[ClinicId] = a.[ClinicId]
                WHERE u.[Id] IS NULL
            )
                THROW 51301, 'Cannot enforce audit tenant ownership because cross-clinic actor references exist.', 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Validation only; no schema or data changes to revert.
    }
}
