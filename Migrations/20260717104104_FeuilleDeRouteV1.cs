using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class FeuilleDeRouteV1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeuilleDeRoutes",
                columns: table => new
                {
                    IdFeuilleDeRoute = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdVoyage = table.Column<int>(type: "int", nullable: false),
                    DateEmbarquement = table.Column<DateTime>(type: "date", nullable: false),
                    DateGenerationUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IdUtilisateurGeneration = table.Column<int>(type: "int", nullable: true),
                    SocieteNom = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SocieteTelephone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SocieteEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SocieteAdresse = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SocieteLogo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VoyageDateDepart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    VoyageHeureDepart = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    VoyagePrix = table.Column<int>(type: "int", nullable: false),
                    VoyageCodeDevise = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdDestination = table.Column<int>(type: "int", nullable: false),
                    DestinationLibelle = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdVehicule = table.Column<int>(type: "int", nullable: false),
                    VehiculeImmatriculation = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VehiculeAlias = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    SiteNom = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombrePassagers = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeuilleDeRoutes", x => x.IdFeuilleDeRoute);
                    table.ForeignKey(
                        name: "FK_FeuilleDeRoutes_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeuilleDeRoutes_Utilisateurs_IdUtilisateurGeneration",
                        column: x => x.IdUtilisateurGeneration,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeuilleDeRoutes_Voyages_IdVoyage",
                        column: x => x.IdVoyage,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FeuilleDeRoutePassagers",
                columns: table => new
                {
                    IdFeuilleDeRoutePassager = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdFeuilleDeRoute = table.Column<int>(type: "int", nullable: false),
                    IdEmbarquement = table.Column<int>(type: "int", nullable: true),
                    IdBillet = table.Column<int>(type: "int", nullable: true),
                    IdReservationPassenger = table.Column<int>(type: "int", nullable: true),
                    IdReservation = table.Column<int>(type: "int", nullable: true),
                    NomComplet = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telephone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentNumero = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeSiege = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateEmbarquementUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IdUtilisateurEnregistrement = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeuilleDeRoutePassagers", x => x.IdFeuilleDeRoutePassager);
                    table.ForeignKey(
                        name: "FK_FeuilleDeRoutePassagers_FeuilleDeRoutes_IdFeuilleDeRoute",
                        column: x => x.IdFeuilleDeRoute,
                        principalTable: "FeuilleDeRoutes",
                        principalColumn: "IdFeuilleDeRoute",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FeuilleDeRoutePassagers_IdFeuilleDeRoute",
                table: "FeuilleDeRoutePassagers",
                column: "IdFeuilleDeRoute");

            migrationBuilder.CreateIndex(
                name: "IX_FeuilleDeRoutes_IdSociete",
                table: "FeuilleDeRoutes",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_FeuilleDeRoutes_IdUtilisateurGeneration",
                table: "FeuilleDeRoutes",
                column: "IdUtilisateurGeneration");

            migrationBuilder.CreateIndex(
                name: "IX_FeuilleDeRoutes_IdVoyage",
                table: "FeuilleDeRoutes",
                column: "IdVoyage");

            migrationBuilder.CreateIndex(
                name: "IX_FeuilleDeRoutes_Societe_DateEmbarquement",
                table: "FeuilleDeRoutes",
                columns: new[] { "IdSociete", "DateEmbarquement" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeuilleDeRoutePassagers");

            migrationBuilder.DropTable(
                name: "FeuilleDeRoutes");
        }
    }
}
