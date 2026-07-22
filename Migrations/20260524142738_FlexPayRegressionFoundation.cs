using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class FlexPayRegressionFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StatutPaiementMetier",
                table: "Paiements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommandesReservationEnAttente",
                columns: table => new
                {
                    IdCommandeReservationEnAttente = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    MethodePaiement = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantVoyage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDeviseVoyage = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantFlexPay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevisePaiement = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TauxVersDevisePaiement = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    OrderNumberFlexPay = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceFlexPay = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadMetierJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdPaiementEnAttente = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandesReservationEnAttente", x => x.IdCommandeReservationEnAttente);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiegeHoldsEnAttente",
                columns: table => new
                {
                    IdSiegeHoldEnAttente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdVoyage = table.Column<int>(type: "int", nullable: false),
                    IdSiege = table.Column<int>(type: "int", nullable: false),
                    IdCommandeReservationEnAttente = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ExpireAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiegeHoldsEnAttente", x => x.IdSiegeHoldEnAttente);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CommandesReservationEnAttente_OrderNumber",
                table: "CommandesReservationEnAttente",
                column: "OrderNumberFlexPay",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandesReservationEnAttente_Societe_Date",
                table: "CommandesReservationEnAttente",
                columns: new[] { "IdSociete", "DateCreation" });

            migrationBuilder.CreateIndex(
                name: "IX_SiegeHoldsEnAttente_IdCommande",
                table: "SiegeHoldsEnAttente",
                column: "IdCommandeReservationEnAttente");

            migrationBuilder.CreateIndex(
                name: "IX_SiegeHoldsEnAttente_Voyage_ExpireAt",
                table: "SiegeHoldsEnAttente",
                columns: new[] { "IdVoyage", "ExpireAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SiegeHoldsEnAttente_Voyage_Siege_Unique",
                table: "SiegeHoldsEnAttente",
                columns: new[] { "IdVoyage", "IdSiege" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandesReservationEnAttente");

            migrationBuilder.DropTable(
                name: "SiegeHoldsEnAttente");

            migrationBuilder.DropColumn(
                name: "StatutPaiementMetier",
                table: "Paiements");
        }
    }
}
