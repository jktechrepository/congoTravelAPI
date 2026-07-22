using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class OrigineOperationReservationPaiement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origine",
                table: "Reservations",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "INCONNU")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Origine",
                table: "Paiements",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "INCONNU")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Origine",
                table: "CommandesReservationEnAttente",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "INCONNU")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origine",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Origine",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "Origine",
                table: "CommandesReservationEnAttente");
        }
    }
}
