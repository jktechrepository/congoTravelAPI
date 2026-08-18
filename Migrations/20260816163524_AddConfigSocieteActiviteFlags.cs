using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddConfigSocieteActiviteFlags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ActiviteEvenement",
                table: "ConfigSocietes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ActiviteRestaurant",
                table: "ConfigSocietes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ActiviteSiteTouristique",
                table: "ConfigSocietes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ActiviteTransport",
                table: "ConfigSocietes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiviteEvenement",
                table: "ConfigSocietes");

            migrationBuilder.DropColumn(
                name: "ActiviteRestaurant",
                table: "ConfigSocietes");

            migrationBuilder.DropColumn(
                name: "ActiviteSiteTouristique",
                table: "ConfigSocietes");

            migrationBuilder.DropColumn(
                name: "ActiviteTransport",
                table: "ConfigSocietes");
        }
    }
}
