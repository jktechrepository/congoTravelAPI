using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class SiteNumeroMobileMoney : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumeroMobileMoney",
                table: "Sites",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroMobileMoney",
                table: "Sites");
        }
    }
}
