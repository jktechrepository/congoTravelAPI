using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddConfigSocieteHeuresOuvertureEntreeEvenement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HeuresOuvertureEntreeEvenementAvantDebut",
                table: "ConfigSocietes",
                type: "int",
                nullable: false,
                defaultValue: 3);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeuresOuvertureEntreeEvenementAvantDebut",
                table: "ConfigSocietes");
        }
    }
}
