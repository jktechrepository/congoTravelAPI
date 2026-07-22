using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class DestinationSocieteVillesUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Destinations_Societe_Villes_Unique",
                table: "Destinations",
                columns: new[] { "IdSociete", "VilleDepart", "VilleArrivee" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Destinations_Societe_Villes_Unique",
                table: "Destinations");
        }
    }
}
