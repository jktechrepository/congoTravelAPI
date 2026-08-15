using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddSiteTouristiqueLieuHorairesFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "HeureFermeture",
                table: "SiteTouristiques",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HeureOuverture",
                table: "SiteTouristiques",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JourOuverture",
                table: "SiteTouristiques",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeureFermeture",
                table: "SiteTouristiques");

            migrationBuilder.DropColumn(
                name: "HeureOuverture",
                table: "SiteTouristiques");

            migrationBuilder.DropColumn(
                name: "JourOuverture",
                table: "SiteTouristiques");
        }
    }
}
