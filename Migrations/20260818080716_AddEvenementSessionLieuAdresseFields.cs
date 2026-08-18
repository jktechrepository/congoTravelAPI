using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddEvenementSessionLieuAdresseFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Avenue",
                table: "EvenementSessions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Commune",
                table: "EvenementSessions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Numero",
                table: "EvenementSessions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Quartier",
                table: "EvenementSessions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Ville",
                table: "EvenementSessions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Avenue",
                table: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "Commune",
                table: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "Quartier",
                table: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "Ville",
                table: "EvenementSessions");
        }
    }
}
