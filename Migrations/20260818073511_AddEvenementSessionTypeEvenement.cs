using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddEvenementSessionTypeEvenement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TypeEvenement",
                table: "EvenementSessions",
                type: "enum('Sport','Music','Art','Cinema','Formation','Conference','Spectacle','Festival','Autres')",
                nullable: false,
                defaultValue: "Autres")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypeEvenement",
                table: "EvenementSessions");
        }
    }
}
