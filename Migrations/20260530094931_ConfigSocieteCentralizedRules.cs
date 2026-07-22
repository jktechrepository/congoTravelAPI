using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class ConfigSocieteCentralizedRules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfigSocietes",
                columns: table => new
                {
                    IdConfigSociete = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    DureeValiditeBilletJours = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PenaliteReaffectation = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    JoursAvanceMaxReservation = table.Column<int>(type: "int", nullable: true),
                    HeuresLimiteReaffectation = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    HeuresOuvertureEmbarquementAvantDepart = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    HeuresFermetureEmbarquementApresJourDepart = table.Column<int>(type: "int", nullable: false, defaultValue: 24),
                    DureeHoldFlexPayMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    ReaffectationActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigSocietes", x => x.IdConfigSociete);
                    table.ForeignKey(
                        name: "FK_ConfigSocietes_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigSociete_IdSociete_Unique",
                table: "ConfigSocietes",
                column: "IdSociete",
                unique: true);

            // Backfill depuis le voyage le plus récent par société, puis défauts pour sociétés sans voyage.
            migrationBuilder.Sql(@"
INSERT INTO ConfigSocietes (
    IdSociete,
    DureeValiditeBilletJours,
    PenaliteReaffectation,
    JoursAvanceMaxReservation,
    HeuresLimiteReaffectation,
    HeuresOuvertureEmbarquementAvantDepart,
    HeuresFermetureEmbarquementApresJourDepart,
    DureeHoldFlexPayMinutes,
    ReaffectationActive,
    DateCreation
)
SELECT
    s.IdSociete,
    COALESCE(v.DureeValiditeBilletJours, 0),
    COALESCE(v.PenaliteReaffectation, 0),
    NULL,
    COALESCE(v.HeuresLimiteReaffectation, 2),
    3,
    24,
    15,
    1,
    UTC_TIMESTAMP(6)
FROM Societes s
LEFT JOIN (
    SELECT v1.IdSociete,
           v1.DureeValiditeBilletJours,
           v1.PenaliteReaffectation,
           v1.HeuresLimiteReaffectation
    FROM Voyages v1
    INNER JOIN (
        SELECT v2.IdSociete, MAX(v2.Id) AS IdVoyageRetenu
        FROM Voyages v2
        INNER JOIN (
            SELECT IdSociete, MAX(DateCreation) AS MaxDateCreation
            FROM Voyages
            GROUP BY IdSociete
        ) m ON m.IdSociete = v2.IdSociete AND v2.DateCreation = m.MaxDateCreation
        GROUP BY v2.IdSociete
    ) pick ON pick.IdVoyageRetenu = v1.Id
) v ON v.IdSociete = s.IdSociete;
");

            migrationBuilder.DropColumn(
                name: "DureeValiditeBilletJours",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "HeuresLimiteReaffectation",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "PenaliteReaffectation",
                table: "Voyages");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DureeValiditeBilletJours",
                table: "Voyages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HeuresLimiteReaffectation",
                table: "Voyages",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaliteReaffectation",
                table: "Voyages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
UPDATE Voyages v
INNER JOIN ConfigSocietes c ON c.IdSociete = v.IdSociete
SET v.DureeValiditeBilletJours = c.DureeValiditeBilletJours,
    v.PenaliteReaffectation = c.PenaliteReaffectation,
    v.HeuresLimiteReaffectation = c.HeuresLimiteReaffectation;
");

            migrationBuilder.DropTable(
                name: "ConfigSocietes");
        }
    }
}
