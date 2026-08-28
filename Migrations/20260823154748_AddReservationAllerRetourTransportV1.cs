using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddReservationAllerRetourTransportV1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllerRetourLeg",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdReservationAllerRetour",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdReservationAllerRetour",
                table: "Paiements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeCommande",
                table: "CommandesReservationEnAttente",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Single")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReservationsAllerRetour",
                columns: table => new
                {
                    IdReservationAllerRetour = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdVoyageAller = table.Column<int>(type: "int", nullable: false),
                    IdVoyageRetour = table.Column<int>(type: "int", nullable: false),
                    IdReservationAller = table.Column<int>(type: "int", nullable: true),
                    IdReservationRetour = table.Column<int>(type: "int", nullable: true),
                    IdPaiement = table.Column<int>(type: "int", nullable: true),
                    IdCommandeReservationEnAttente = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Statut = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    Origine = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationsAllerRetour", x => x.IdReservationAllerRetour);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IdReservationAllerRetour",
                table: "Reservations",
                column: "IdReservationAllerRetour");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationsAllerRetour_IdSociete",
                table: "ReservationsAllerRetour",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationsAllerRetour_Statut",
                table: "ReservationsAllerRetour",
                column: "Statut");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_ReservationsAllerRetour_IdReservationAllerRetour",
                table: "Reservations",
                column: "IdReservationAllerRetour",
                principalTable: "ReservationsAllerRetour",
                principalColumn: "IdReservationAllerRetour",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_ReservationsAllerRetour_IdReservationAllerRetour",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "ReservationsAllerRetour");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_IdReservationAllerRetour",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "AllerRetourLeg",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "IdReservationAllerRetour",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "IdReservationAllerRetour",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "TypeCommande",
                table: "CommandesReservationEnAttente");
        }
    }
}
