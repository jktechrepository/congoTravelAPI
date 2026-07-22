using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class ReversementAutoPaiementElectronique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdPaiement",
                table: "ReversementsSite",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdReservation",
                table: "ReversementsSite",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origine",
                table: "ReversementsSite",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Manuel")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "AutoReversementPaiementElectronique",
                table: "ConfigSocietes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ReversementSite_IdPaiement",
                table: "ReversementsSite",
                column: "IdPaiement",
                unique: true,
                filter: "[IdPaiement] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReversementSite_IdPaiement",
                table: "ReversementsSite");

            migrationBuilder.DropColumn(
                name: "IdPaiement",
                table: "ReversementsSite");

            migrationBuilder.DropColumn(
                name: "IdReservation",
                table: "ReversementsSite");

            migrationBuilder.DropColumn(
                name: "Origine",
                table: "ReversementsSite");

            migrationBuilder.DropColumn(
                name: "AutoReversementPaiementElectronique",
                table: "ConfigSocietes");
        }
    }
}
