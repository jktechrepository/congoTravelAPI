using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class PenaliteReaffectationBillet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PenaliteReaffectation",
                table: "Voyages",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaliteOverride",
                table: "Billets",
                type: "decimal(18,2)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PenaliteReaffectation",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "PenaliteOverride",
                table: "Billets");
        }
    }
}
