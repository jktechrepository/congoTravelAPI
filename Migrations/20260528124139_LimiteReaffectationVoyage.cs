using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class LimiteReaffectationVoyage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HeuresLimiteReaffectation",
                table: "Voyages",
                type: "int",
                nullable: false,
                defaultValue: 2);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeuresLimiteReaffectation",
                table: "Voyages");
        }
    }
}
