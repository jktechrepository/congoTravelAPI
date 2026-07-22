using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class PlanificationVoyageV1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdPlanificationVoyage",
                table: "Voyages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanificationsVoyage",
                columns: table => new
                {
                    IdPlanificationVoyage = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Libelle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: false),
                    IdVehicule = table.Column<int>(type: "int", nullable: false),
                    HeureDepart = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Prix = table.Column<int>(type: "int", nullable: false),
                    CodeDevisePrix = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JoursSemaine = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanificationsVoyage", x => x.IdPlanificationVoyage);
                    table.ForeignKey(
                        name: "FK_PlanificationsVoyage_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanificationsVoyage_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanificationsVoyage_Vehicules_IdVehicule",
                        column: x => x.IdVehicule,
                        principalTable: "Vehicules",
                        principalColumn: "IdVehicule",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlanificationGenerationLogs",
                columns: table => new
                {
                    IdPlanificationGenerationLog = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdPlanificationVoyage = table.Column<int>(type: "int", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NombreCrees = table.Column<int>(type: "int", nullable: false),
                    NombreIgnores = table.Column<int>(type: "int", nullable: false),
                    NombreEchecs = table.Column<int>(type: "int", nullable: false),
                    DetailsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeclencheParIdUtilisateur = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanificationGenerationLogs", x => x.IdPlanificationGenerationLog);
                    table.ForeignKey(
                        name: "FK_PlanificationGenerationLogs_PlanificationsVoyage_IdPlanifica~",
                        column: x => x.IdPlanificationVoyage,
                        principalTable: "PlanificationsVoyage",
                        principalColumn: "IdPlanificationVoyage",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlanificationVoyageEtapes",
                columns: table => new
                {
                    IdPlanificationVoyageEtape = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdPlanificationVoyage = table.Column<int>(type: "int", nullable: false),
                    IdDestination = table.Column<int>(type: "int", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanificationVoyageEtapes", x => x.IdPlanificationVoyageEtape);
                    table.ForeignKey(
                        name: "FK_PlanificationVoyageEtapes_Destinations_IdDestination",
                        column: x => x.IdDestination,
                        principalTable: "Destinations",
                        principalColumn: "IdDestination",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanificationVoyageEtapes_PlanificationsVoyage_IdPlanificati~",
                        column: x => x.IdPlanificationVoyage,
                        principalTable: "PlanificationsVoyage",
                        principalColumn: "IdPlanificationVoyage",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlanificationVoyageTarifs",
                columns: table => new
                {
                    IdPlanificationVoyageTarif = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdPlanificationVoyage = table.Column<int>(type: "int", nullable: false),
                    IdCategorieSiege = table.Column<int>(type: "int", nullable: false),
                    Prix = table.Column<int>(type: "int", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanificationVoyageTarifs", x => x.IdPlanificationVoyageTarif);
                    table.ForeignKey(
                        name: "FK_PlanificationVoyageTarifs_CategorieSieges_IdCategorieSiege",
                        column: x => x.IdCategorieSiege,
                        principalTable: "CategorieSieges",
                        principalColumn: "IdCategorieSiege",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanificationVoyageTarifs_PlanificationsVoyage_IdPlanificati~",
                        column: x => x.IdPlanificationVoyage,
                        principalTable: "PlanificationsVoyage",
                        principalColumn: "IdPlanificationVoyage",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Voyages_IdPlanificationVoyage",
                table: "Voyages",
                column: "IdPlanificationVoyage");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificationGenerationLogs_IdPlanificationVoyage",
                table: "PlanificationGenerationLogs",
                column: "IdPlanificationVoyage");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificationsVoyage_IdSite",
                table: "PlanificationsVoyage",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificationsVoyage_IdSociete",
                table: "PlanificationsVoyage",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificationsVoyage_IdVehicule",
                table: "PlanificationsVoyage",
                column: "IdVehicule");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificationVoyageEtapes_IdDestination",
                table: "PlanificationVoyageEtapes",
                column: "IdDestination");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificationVoyageEtapes_Planif_Ordre_Unique",
                table: "PlanificationVoyageEtapes",
                columns: new[] { "IdPlanificationVoyage", "Ordre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanificationVoyageTarifs_IdCategorieSiege",
                table: "PlanificationVoyageTarifs",
                column: "IdCategorieSiege");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificationVoyageTarifs_Planif_Categorie_Unique",
                table: "PlanificationVoyageTarifs",
                columns: new[] { "IdPlanificationVoyage", "IdCategorieSiege" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Voyages_PlanificationsVoyage_IdPlanificationVoyage",
                table: "Voyages",
                column: "IdPlanificationVoyage",
                principalTable: "PlanificationsVoyage",
                principalColumn: "IdPlanificationVoyage",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Voyages_PlanificationsVoyage_IdPlanificationVoyage",
                table: "Voyages");

            migrationBuilder.DropTable(
                name: "PlanificationGenerationLogs");

            migrationBuilder.DropTable(
                name: "PlanificationVoyageEtapes");

            migrationBuilder.DropTable(
                name: "PlanificationVoyageTarifs");

            migrationBuilder.DropTable(
                name: "PlanificationsVoyage");

            migrationBuilder.DropIndex(
                name: "IX_Voyages_IdPlanificationVoyage",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "IdPlanificationVoyage",
                table: "Voyages");
        }
    }
}
