using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AuranClinicDbContext))]
[Migration("20261014194500_CreateFileDeletionOutbox")]
public sealed class CreateFileDeletionOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Operational SQL outbox intentionally has no EF entity: SQL locking and
        // claims must never be filtered through the authenticated clinic DbContext.
        migrationBuilder.Sql("""
            CREATE TABLE dbo.FileDeletionOutbox (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_FileDeletionOutbox PRIMARY KEY,
                ClinicId uniqueidentifier NOT NULL,
                StorageProvider nvarchar(32) NOT NULL,
                StorageKey nvarchar(512) NOT NULL,
                CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_FileDeletionOutbox_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
                NotBeforeUtc datetime2 NOT NULL,
                LeaseUntilUtc datetime2 NULL,
                AttemptCount int NOT NULL CONSTRAINT DF_FileDeletionOutbox_AttemptCount DEFAULT 0,
                CompletedAtUtc datetime2 NULL,
                LastError nvarchar(512) NULL,
                CONSTRAINT CK_FileDeletionOutbox_Attempts CHECK (AttemptCount >= 0)
            );
            CREATE INDEX IX_FileDeletionOutbox_Pending
                ON dbo.FileDeletionOutbox (CompletedAtUtc, NotBeforeUtc, LeaseUntilUtc)
                INCLUDE (StorageProvider, StorageKey, AttemptCount);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE dbo.FileDeletionOutbox;");
    }
}
