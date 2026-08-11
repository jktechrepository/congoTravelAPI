using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddEvenementReservationIdUtilisateur : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdUtilisateur",
                table: "EvenementReservations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservations_IdUtilisateur",
                table: "EvenementReservations",
                column: "IdUtilisateur");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EvenementReservations_IdUtilisateur",
                table: "EvenementReservations");

            migrationBuilder.DropColumn(
                name: "IdUtilisateur",
                table: "EvenementReservations");
        }
    }
}
