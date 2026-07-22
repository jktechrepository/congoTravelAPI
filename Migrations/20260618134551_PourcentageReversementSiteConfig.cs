using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class PourcentageReversementSiteConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PourcentageReversementSite",
                table: "ConfigSocietes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 100m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PourcentageReversementSite",
                table: "ConfigSocietes");
        }
    }
}
