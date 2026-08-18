using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddEvenementSessionOrganisateurFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MailOrganisateur",
                table: "EvenementSessions",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NomOrganisateur",
                table: "EvenementSessions",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TelephoneOrganisateur",
                table: "EvenementSessions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MailOrganisateur",
                table: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "NomOrganisateur",
                table: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "TelephoneOrganisateur",
                table: "EvenementSessions");
        }
    }
}
