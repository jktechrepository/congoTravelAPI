using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class FraisPlateformeConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeDeviseFraisPlateforme",
                table: "ConfigSocietes",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "FraisPlateforme",
                table: "ConfigSocietes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeDeviseFraisPlateforme",
                table: "ConfigSocietes");

            migrationBuilder.DropColumn(
                name: "FraisPlateforme",
                table: "ConfigSocietes");
        }
    }
}
