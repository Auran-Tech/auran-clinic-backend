using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auran.Clinic.Infrastructure.Persistence.Migrations
{
    public partial class AddClinicalOrderSectionDefinitionCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderSectionDefinitions_ClinicId",
                table: "ClinicalOrderSectionDefinitions");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ClinicalOrderSectionDefinitions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE ClinicalOrderSectionDefinitions
                SET Code = UPPER(REPLACE(REPLACE(REPLACE(Name, ' ', '_'), '-', '_'), '.', '_'))
                WHERE Code = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderSectionDefinitions_ClinicId_Code",
                table: "ClinicalOrderSectionDefinitions",
                columns: new[] { "ClinicId", "Code" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicalOrderSectionDefinitions_ClinicId_Code",
                table: "ClinicalOrderSectionDefinitions");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ClinicalOrderSectionDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalOrderSectionDefinitions_ClinicId",
                table: "ClinicalOrderSectionDefinitions",
                column: "ClinicId");
        }
    }
}
