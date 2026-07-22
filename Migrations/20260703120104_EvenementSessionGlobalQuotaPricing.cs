using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class EvenementSessionGlobalQuotaPricing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeDevise",
                table: "EvenementSessionGlobalQuotas",
                type: "char(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "CDF")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "PrixUnitaire",
                table: "EvenementSessionGlobalQuotas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeDevise",
                table: "EvenementSessionGlobalQuotas");

            migrationBuilder.DropColumn(
                name: "PrixUnitaire",
                table: "EvenementSessionGlobalQuotas");
        }
    }
}
