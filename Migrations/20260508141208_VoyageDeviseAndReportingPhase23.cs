using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class VoyageDeviseAndReportingPhase23 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrincipale",
                table: "Voyages",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "CDF")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrix",
                table: "Voyages",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "CDF")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "PrixDevisePrincipale",
                table: "Voyages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxVersDevisePrincipale",
                table: "Voyages",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.CreateTable(
                name: "Remboursements",
                columns: table => new
                {
                    IdRemboursement = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdPaiement = table.Column<int>(type: "int", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    CodeDeviseRemboursement = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeDevisePrincipale = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantRembourse = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TauxVersDevisePrincipale = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    MontantRembourseDevisePrincipale = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DateRemboursement = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Motif = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remboursements", x => x.IdRemboursement);
                    table.ForeignKey(
                        name: "FK_Remboursements_Paiements_IdPaiement",
                        column: x => x.IdPaiement,
                        principalTable: "Paiements",
                        principalColumn: "IdPaiement",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Voyages_Societe_DevisePrix_Date",
                table: "Voyages",
                columns: new[] { "IdSociete", "CodeDevisePrix", "date_depart" });

            migrationBuilder.CreateIndex(
                name: "IX_Remboursements_IdPaiement",
                table: "Remboursements",
                column: "IdPaiement");

            migrationBuilder.CreateIndex(
                name: "IX_Remboursements_Societe_Date",
                table: "Remboursements",
                columns: new[] { "IdSociete", "DateRemboursement" });

            migrationBuilder.Sql(@"
                UPDATE Voyages v
                LEFT JOIN Societes s ON s.IdSociete = v.IdSociete
                SET
                    v.CodeDevisePrix = CASE
                        WHEN v.CodeDevisePrix IS NULL OR v.CodeDevisePrix = '' THEN 'CDF'
                        ELSE v.CodeDevisePrix
                    END,
                    v.CodeDevisePrincipale = CASE
                        WHEN v.CodeDevisePrincipale IS NULL OR v.CodeDevisePrincipale = '' THEN COALESCE(NULLIF(s.CodeDevisePrincipale, ''), 'CDF')
                        ELSE v.CodeDevisePrincipale
                    END,
                    v.TauxVersDevisePrincipale = CASE
                        WHEN v.TauxVersDevisePrincipale IS NULL OR v.TauxVersDevisePrincipale = 0 THEN 1
                        ELSE v.TauxVersDevisePrincipale
                    END,
                    v.PrixDevisePrincipale = CASE
                        WHEN v.PrixDevisePrincipale IS NULL OR v.PrixDevisePrincipale = 0 THEN v.Prix
                        ELSE v.PrixDevisePrincipale
                    END;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Remboursements");

            migrationBuilder.DropIndex(
                name: "IX_Voyages_Societe_DevisePrix_Date",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrincipale",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrix",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "PrixDevisePrincipale",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "TauxVersDevisePrincipale",
                table: "Voyages");
        }
    }
}
