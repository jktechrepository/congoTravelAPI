using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddEvenementReservationIdClient : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdClient",
                table: "EvenementReservations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservations_IdClient",
                table: "EvenementReservations",
                column: "IdClient");

            migrationBuilder.AddForeignKey(
                name: "FK_EvenementReservations_Clients_IdClient",
                table: "EvenementReservations",
                column: "IdClient",
                principalTable: "Clients",
                principalColumn: "IdClient",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvenementReservations_Clients_IdClient",
                table: "EvenementReservations");

            migrationBuilder.DropIndex(
                name: "IX_EvenementReservations_IdClient",
                table: "EvenementReservations");

            migrationBuilder.DropColumn(
                name: "IdClient",
                table: "EvenementReservations");
        }
    }
}
