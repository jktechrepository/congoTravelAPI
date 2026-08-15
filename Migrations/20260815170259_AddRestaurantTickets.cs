using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddRestaurantTickets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HeuresOuvertureEntreeRestaurantAvantDebut",
                table: "ConfigSocietes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "RestaurantTickets",
                columns: table => new
                {
                    IdRestaurantTicket = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdRestaurantReservationLine = table.Column<int>(type: "int", nullable: false),
                    TicketCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "enum('ISSUED','USED','VOID')", nullable: false, defaultValue: "ISSUED")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantTickets", x => x.IdRestaurantTicket);
                    table.ForeignKey(
                        name: "FK_RestaurantTickets_RestaurantReservationLines_IdRestaurantRes~",
                        column: x => x.IdRestaurantReservationLine,
                        principalTable: "RestaurantReservationLines",
                        principalColumn: "IdRestaurantReservationLine",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTickets_IdRestaurantReservationLine",
                table: "RestaurantTickets",
                column: "IdRestaurantReservationLine");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTickets_Status",
                table: "RestaurantTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTickets_TicketCode_UQ",
                table: "RestaurantTickets",
                column: "TicketCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantTickets");

            migrationBuilder.DropColumn(
                name: "HeuresOuvertureEntreeRestaurantAvantDebut",
                table: "ConfigSocietes");
        }
    }
}
