using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddIdSiteToVoyages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdSite",
                table: "Voyages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Voyages_IdSite",
                table: "Voyages",
                column: "IdSite");

            migrationBuilder.AddForeignKey(
                name: "FK_Voyages_Sites_IdSite",
                table: "Voyages",
                column: "IdSite",
                principalTable: "Sites",
                principalColumn: "IdSite",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Voyages_Sites_IdSite",
                table: "Voyages");

            migrationBuilder.DropIndex(
                name: "IX_Voyages_IdSite",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "IdSite",
                table: "Voyages");
        }
    }
}
