using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class ConfigSocietePenalitePourcentage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PenaliteReaffectation",
                table: "ConfigSocietes",
                newName: "PenaliteReaffectationPourcentage");

            // Anciennes valeurs = montants fixes ; repartir à 0 % pour reconfiguration manuelle.
            migrationBuilder.Sql(
                "UPDATE `ConfigSocietes` SET `PenaliteReaffectationPourcentage` = 0 WHERE `PenaliteReaffectationPourcentage` <> 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PenaliteReaffectationPourcentage",
                table: "ConfigSocietes",
                newName: "PenaliteReaffectation");
        }
    }
}
