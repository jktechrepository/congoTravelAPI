using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class ReversementSiteFlexPayPayOut : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReversementsSite",
                columns: table => new
                {
                    IdReversementSite = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSite = table.Column<int>(type: "int", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    NumeroMobileMoney = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevise = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderReference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeMarchand = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    CodeFlexPay = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageFlexPay = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Channel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Motif = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateCallback = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReversementsSite", x => x.IdReversementSite);
                    table.ForeignKey(
                        name: "FK_ReversementsSite_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReversementsSite_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ReversementSite_OrderNumber",
                table: "ReversementsSite",
                column: "OrderNumber",
                unique: true,
                filter: "[OrderNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReversementSite_Societe_Site_Date",
                table: "ReversementsSite",
                columns: new[] { "IdSociete", "IdSite", "DateCreation" });

            migrationBuilder.CreateIndex(
                name: "IX_ReversementsSite_IdSite",
                table: "ReversementsSite",
                column: "IdSite");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReversementsSite");
        }
    }
}
