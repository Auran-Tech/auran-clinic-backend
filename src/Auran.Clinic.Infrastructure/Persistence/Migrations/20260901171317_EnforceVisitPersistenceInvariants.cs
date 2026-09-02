using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceVisitPersistenceInvariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitSessions_ClinicId",
                table: "VisitSessions");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_ClinicId_VisitId",
                table: "QueueEntries");

            migrationBuilder.DropColumn(
                name: "FollowUpText",
                table: "Visits");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Visits",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_ClinicId_VisitId",
                table: "VisitSessions",
                columns: new[] { "ClinicId", "VisitId" },
                unique: true,
                filter: "[EndedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_ClinicId_VisitId",
                table: "QueueEntries",
                columns: new[] { "ClinicId", "VisitId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitSessions_ClinicId_VisitId",
                table: "VisitSessions");

            migrationBuilder.DropIndex(
                name: "IX_QueueEntries_ClinicId_VisitId",
                table: "QueueEntries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Visits");

            migrationBuilder.AddColumn<string>(
                name: "FollowUpText",
                table: "Visits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_ClinicId",
                table: "VisitSessions",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueEntries_ClinicId_VisitId",
                table: "QueueEntries",
                columns: new[] { "ClinicId", "VisitId" });
        }
    }
}
