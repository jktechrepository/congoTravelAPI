using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class BilletValiditeMultiVoyages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DureeValiditeBilletJours",
                table: "Voyages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateValiditeDebut",
                table: "Billets",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateValiditeFin",
                table: "Billets",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Billets b
                INNER JOIN Reservations r ON r.IdReservation = b.IdReservation
                INNER JOIN Voyages v ON v.Id = r.IdVoyage
                SET
                    b.DateValiditeDebut = COALESCE(b.DateValiditeDebut, DATE(v.date_depart)),
                    b.DateValiditeFin = COALESCE(b.DateValiditeFin, DATE_ADD(DATE(v.date_depart), INTERVAL GREATEST(v.DureeValiditeBilletJours, 0) DAY))
                WHERE b.IdReservation IS NOT NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DureeValiditeBilletJours",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "DateValiditeDebut",
                table: "Billets");

            migrationBuilder.DropColumn(
                name: "DateValiditeFin",
                table: "Billets");
        }
    }
}
