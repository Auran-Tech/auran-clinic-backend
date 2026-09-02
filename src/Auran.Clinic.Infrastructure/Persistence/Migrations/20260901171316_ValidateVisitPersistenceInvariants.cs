using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AuranClinicDbContext))]
[Migration("20260901171316_ValidateVisitPersistenceInvariants")]
public sealed class ValidateVisitPersistenceInvariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM [Visits]
                WHERE [FollowUpText] IS NOT NULL
                    AND LEN(LTRIM(RTRIM([FollowUpText]))) > 0
            )
                THROW 51401, 'Cannot remove Visits.FollowUpText because legacy follow-up data exists. Migrate it to FollowUps before deployment.', 1;

            IF EXISTS (
                SELECT 1
                FROM [QueueEntries]
                GROUP BY [ClinicId], [VisitId]
                HAVING COUNT(*) > 1
            )
                THROW 51402, 'Cannot enforce one queue entry per visit because duplicate queue entries exist.', 1;

            IF EXISTS (
                SELECT 1
                FROM [VisitSessions]
                WHERE [EndedAtUtc] IS NULL
                GROUP BY [ClinicId], [VisitId]
                HAVING COUNT(*) > 1
            )
                THROW 51403, 'Cannot enforce one active visit session because duplicate active sessions exist.', 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Validation only; no schema or data changes to revert.
    }
}
