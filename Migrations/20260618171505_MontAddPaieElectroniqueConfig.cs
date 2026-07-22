using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class MontAddPaieElectroniqueConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeDeviseMontAddPaieElectronique",
                table: "ConfigSocietes",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MontAddPaieElectronique",
                table: "ConfigSocietes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeDeviseMontAddPaieElectronique",
                table: "ConfigSocietes");

            migrationBuilder.DropColumn(
                name: "MontAddPaieElectronique",
                table: "ConfigSocietes");
        }
    }
}
