using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddUniqueDeviseBySociete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DevisesMonetaires_CodeDevise_Unique",
                table: "DevisesMonetaires");

            migrationBuilder.CreateIndex(
                name: "IX_DevisesMonetaires_Societe_CodeDevise_Unique",
                table: "DevisesMonetaires",
                columns: new[] { "IdSociete", "CodeDevise" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DevisesMonetaires_Societe_CodeDevise_Unique",
                table: "DevisesMonetaires");

            migrationBuilder.CreateIndex(
                name: "IX_DevisesMonetaires_CodeDevise_Unique",
                table: "DevisesMonetaires",
                column: "CodeDevise",
                unique: true);
        }
    }
}
