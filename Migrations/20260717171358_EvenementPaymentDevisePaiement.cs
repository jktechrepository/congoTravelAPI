using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class EvenementPaymentDevisePaiement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeDeviseTarif",
                table: "EvenementPayments",
                type: "char(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "CDF")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MontantTarif",
                table: "EvenementPayments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxVersDevisePaiement",
                table: "EvenementPayments",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 1m);

            // Backfill : paiements existants — tarif = montant FlexPay déjà stocké (pas de conversion V1).
            migrationBuilder.Sql(@"
UPDATE `EvenementPayments`
SET `MontantTarif` = `Montant`,
    `CodeDeviseTarif` = `CodeDevise`,
    `TauxVersDevisePaiement` = 1;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeDeviseTarif",
                table: "EvenementPayments");

            migrationBuilder.DropColumn(
                name: "MontantTarif",
                table: "EvenementPayments");

            migrationBuilder.DropColumn(
                name: "TauxVersDevisePaiement",
                table: "EvenementPayments");
        }
    }
}
