using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddSiteTouristiqueAndRestaurantSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DureeHoldRestaurantMinutes",
                table: "ConfigSocietes",
                type: "int",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<int>(
                name: "DureeHoldSiteTouristiqueMinutes",
                table: "ConfigSocietes",
                type: "int",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.CreateTable(
                name: "Restaurants",
                columns: table => new
                {
                    IdRestaurant = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    CodeRestaurant = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nom = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Adresse = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AcomptePourcentDefaut = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    Status = table.Column<string>(type: "enum('Draft','Published','Closed','Cancelled')", nullable: false, defaultValue: "Draft")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurants", x => x.IdRestaurant);
                    table.ForeignKey(
                        name: "FK_Restaurants_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Restaurants_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiqueClasses",
                columns: table => new
                {
                    IdSiteTouristiqueClasse = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Libelle = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Actif = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiqueClasses", x => x.IdSiteTouristiqueClasse);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueClasses_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiques",
                columns: table => new
                {
                    IdSiteTouristique = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    CodeLieu = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nom = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "enum('Draft','Published','Closed','Cancelled')", nullable: false, defaultValue: "Draft")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiques", x => x.IdSiteTouristique);
                    table.ForeignKey(
                        name: "FK_SiteTouristiques_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiques_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantPlanifications",
                columns: table => new
                {
                    IdRestaurantPlanification = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdRestaurant = table.Column<int>(type: "int", nullable: false),
                    Libelle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JoursSemaine = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InventoryMode = table.Column<string>(type: "enum('GlobalQuota','ClassQuota')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantAcompte = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantPlanifications", x => x.IdRestaurantPlanification);
                    table.ForeignKey(
                        name: "FK_RestaurantPlanifications_Restaurants_IdRestaurant",
                        column: x => x.IdRestaurant,
                        principalTable: "Restaurants",
                        principalColumn: "IdRestaurant",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantPlanifications_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantZones",
                columns: table => new
                {
                    IdRestaurantZone = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdRestaurant = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Libelle = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Actif = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantZones", x => x.IdRestaurantZone);
                    table.ForeignKey(
                        name: "FK_RestaurantZones_Restaurants_IdRestaurant",
                        column: x => x.IdRestaurant,
                        principalTable: "Restaurants",
                        principalColumn: "IdRestaurant",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantZones_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiquePlanifications",
                columns: table => new
                {
                    IdSiteTouristiquePlanification = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSiteTouristique = table.Column<int>(type: "int", nullable: false),
                    Libelle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JoursSemaine = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InventoryMode = table.Column<string>(type: "enum('ClassQuota','GlobalQuota')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SalesOpenOffsetHours = table.Column<int>(type: "int", nullable: true),
                    SalesCloseOffsetHours = table.Column<int>(type: "int", nullable: true),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiquePlanifications", x => x.IdSiteTouristiquePlanification);
                    table.ForeignKey(
                        name: "FK_SiteTouristiquePlanifications_SiteTouristiques_IdSiteTourist~",
                        column: x => x.IdSiteTouristique,
                        principalTable: "SiteTouristiques",
                        principalColumn: "IdSiteTouristique",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiquePlanifications_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantPlanifGenerationLogs",
                columns: table => new
                {
                    IdRestaurantPlanifGenerationLog = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdRestaurantPlanification = table.Column<int>(type: "int", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NombreCrees = table.Column<int>(type: "int", nullable: false),
                    NombreIgnores = table.Column<int>(type: "int", nullable: false),
                    NombreEchecs = table.Column<int>(type: "int", nullable: false),
                    NombrePublies = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DetailsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeclencheParIdUtilisateur = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantPlanifGenerationLogs", x => x.IdRestaurantPlanifGenerationLog);
                    table.ForeignKey(
                        name: "FK_RestaurantPlanifGenerationLogs_RestaurantPlanifications_IdRe~",
                        column: x => x.IdRestaurantPlanification,
                        principalTable: "RestaurantPlanifications",
                        principalColumn: "IdRestaurantPlanification",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantPlanificationPlages",
                columns: table => new
                {
                    IdRestaurantPlanificationPlage = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdRestaurantPlanification = table.Column<int>(type: "int", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Libelle = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantPlanificationPlages", x => x.IdRestaurantPlanificationPlage);
                    table.CheckConstraint("CK_RestaurantPlanificationPlages_StartEnd", "`EndTime` > `StartTime`");
                    table.ForeignKey(
                        name: "FK_RestaurantPlanificationPlages_RestaurantPlanifications_IdRes~",
                        column: x => x.IdRestaurantPlanification,
                        principalTable: "RestaurantPlanifications",
                        principalColumn: "IdRestaurantPlanification",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiqueJournees",
                columns: table => new
                {
                    IdSiteTouristiqueJournee = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSiteTouristique = table.Column<int>(type: "int", nullable: false),
                    DateVisite = table.Column<DateOnly>(type: "date", nullable: false),
                    InventoryMode = table.Column<string>(type: "enum('ClassQuota','GlobalQuota')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "enum('Draft','Published','Closed','Cancelled')", nullable: false, defaultValue: "Draft")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SalesOpenAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SalesCloseAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IdSiteTouristiquePlanification = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiqueJournees", x => x.IdSiteTouristiqueJournee);
                    table.CheckConstraint("CK_SiteTouristiqueJournees_SalesWindow", "`SalesCloseAtUtc` IS NULL OR `SalesOpenAtUtc` IS NULL OR `SalesCloseAtUtc` >= `SalesOpenAtUtc`");
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueJournees_SiteTouristiquePlanifications_IdSite~",
                        column: x => x.IdSiteTouristiquePlanification,
                        principalTable: "SiteTouristiquePlanifications",
                        principalColumn: "IdSiteTouristiquePlanification",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueJournees_SiteTouristiques_IdSiteTouristique",
                        column: x => x.IdSiteTouristique,
                        principalTable: "SiteTouristiques",
                        principalColumn: "IdSiteTouristique",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueJournees_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiquePlanifClassQuotas",
                columns: table => new
                {
                    IdSiteTouristiquePlanifClassQuota = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSiteTouristiquePlanification = table.Column<int>(type: "int", nullable: false),
                    IdSiteTouristiqueClasse = table.Column<int>(type: "int", nullable: false),
                    CapaciteTotale = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiquePlanifClassQuotas", x => x.IdSiteTouristiquePlanifClassQuota);
                    table.CheckConstraint("CK_SiteTouristiquePlanifClassQuotas_Capacite", "`CapaciteTotale` >= 0");
                    table.ForeignKey(
                        name: "FK_SiteTouristiquePlanifClassQuotas_SiteTouristiqueClasses_IdSi~",
                        column: x => x.IdSiteTouristiqueClasse,
                        principalTable: "SiteTouristiqueClasses",
                        principalColumn: "IdSiteTouristiqueClasse",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiquePlanifClassQuotas_SiteTouristiquePlanificatio~",
                        column: x => x.IdSiteTouristiquePlanification,
                        principalTable: "SiteTouristiquePlanifications",
                        principalColumn: "IdSiteTouristiquePlanification",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiquePlanifGenerationLogs",
                columns: table => new
                {
                    IdSiteTouristiquePlanifGenerationLog = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSiteTouristiquePlanification = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SiteTouristiquePlanifGenerationLogs", x => x.IdSiteTouristiquePlanifGenerationLog);
                    table.ForeignKey(
                        name: "FK_SiteTouristiquePlanifGenerationLogs_SiteTouristiquePlanifica~",
                        column: x => x.IdSiteTouristiquePlanification,
                        principalTable: "SiteTouristiquePlanifications",
                        principalColumn: "IdSiteTouristiquePlanification",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiquePlanifGlobalQuotas",
                columns: table => new
                {
                    IdSiteTouristiquePlanification = table.Column<int>(type: "int", nullable: false),
                    CapaciteTotale = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiquePlanifGlobalQuotas", x => x.IdSiteTouristiquePlanification);
                    table.CheckConstraint("CK_SiteTouristiquePlanifGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
                    table.ForeignKey(
                        name: "FK_SiteTouristiquePlanifGlobalQuotas_SiteTouristiquePlanificati~",
                        column: x => x.IdSiteTouristiquePlanification,
                        principalTable: "SiteTouristiquePlanifications",
                        principalColumn: "IdSiteTouristiquePlanification",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantCreneaux",
                columns: table => new
                {
                    IdRestaurantCreneau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdRestaurant = table.Column<int>(type: "int", nullable: false),
                    DateService = table.Column<DateOnly>(type: "date", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    InventoryMode = table.Column<string>(type: "enum('GlobalQuota','ClassQuota')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "enum('Draft','Published','Closed','Cancelled')", nullable: false, defaultValue: "Draft")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantAcompte = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IdRestaurantPlanification = table.Column<int>(type: "int", nullable: true),
                    IdRestaurantPlanificationPlage = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantCreneaux", x => x.IdRestaurantCreneau);
                    table.CheckConstraint("CK_RestaurantCreneaux_StartEnd", "`EndAtUtc` > `StartAtUtc`");
                    table.ForeignKey(
                        name: "FK_RestaurantCreneaux_RestaurantPlanificationPlages_IdRestauran~",
                        column: x => x.IdRestaurantPlanificationPlage,
                        principalTable: "RestaurantPlanificationPlages",
                        principalColumn: "IdRestaurantPlanificationPlage",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantCreneaux_RestaurantPlanifications_IdRestaurantPlan~",
                        column: x => x.IdRestaurantPlanification,
                        principalTable: "RestaurantPlanifications",
                        principalColumn: "IdRestaurantPlanification",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantCreneaux_Restaurants_IdRestaurant",
                        column: x => x.IdRestaurant,
                        principalTable: "Restaurants",
                        principalColumn: "IdRestaurant",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantCreneaux_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantPlanifPlageGlobalQuotas",
                columns: table => new
                {
                    IdRestaurantPlanificationPlage = table.Column<int>(type: "int", nullable: false),
                    CapaciteTotale = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantPlanifPlageGlobalQuotas", x => x.IdRestaurantPlanificationPlage);
                    table.CheckConstraint("CK_RestaurantPlanifPlageGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
                    table.ForeignKey(
                        name: "FK_RestaurantPlanifPlageGlobalQuotas_RestaurantPlanificationPla~",
                        column: x => x.IdRestaurantPlanificationPlage,
                        principalTable: "RestaurantPlanificationPlages",
                        principalColumn: "IdRestaurantPlanificationPlage",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantPlanifPlageZoneQuotas",
                columns: table => new
                {
                    IdRestaurantPlanifPlageZoneQuota = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdRestaurantPlanificationPlage = table.Column<int>(type: "int", nullable: false),
                    IdRestaurantZone = table.Column<int>(type: "int", nullable: false),
                    CapaciteTotale = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantPlanifPlageZoneQuotas", x => x.IdRestaurantPlanifPlageZoneQuota);
                    table.CheckConstraint("CK_RestaurantPlanifPlageZoneQuotas_Capacite", "`CapaciteTotale` >= 0");
                    table.ForeignKey(
                        name: "FK_RestaurantPlanifPlageZoneQuotas_RestaurantPlanificationPlage~",
                        column: x => x.IdRestaurantPlanificationPlage,
                        principalTable: "RestaurantPlanificationPlages",
                        principalColumn: "IdRestaurantPlanificationPlage",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantPlanifPlageZoneQuotas_RestaurantZones_IdRestaurant~",
                        column: x => x.IdRestaurantZone,
                        principalTable: "RestaurantZones",
                        principalColumn: "IdRestaurantZone",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiqueClassQuotas",
                columns: table => new
                {
                    IdSiteTouristiqueClassQuota = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSiteTouristiqueJournee = table.Column<int>(type: "int", nullable: false),
                    IdSiteTouristiqueClasse = table.Column<int>(type: "int", nullable: false),
                    CapaciteTotale = table.Column<int>(type: "int", nullable: false),
                    QuantiteHold = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    QuantiteVendue = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiqueClassQuotas", x => x.IdSiteTouristiqueClassQuota);
                    table.CheckConstraint("CK_SiteTouristiqueClassQuotas_Capacite", "`CapaciteTotale` >= 0");
                    table.CheckConstraint("CK_SiteTouristiqueClassQuotas_StockMax", "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
                    table.CheckConstraint("CK_SiteTouristiqueClassQuotas_StockPositive", "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueClassQuotas_SiteTouristiqueClasses_IdSiteTour~",
                        column: x => x.IdSiteTouristiqueClasse,
                        principalTable: "SiteTouristiqueClasses",
                        principalColumn: "IdSiteTouristiqueClasse",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueClassQuotas_SiteTouristiqueJournees_IdSiteTou~",
                        column: x => x.IdSiteTouristiqueJournee,
                        principalTable: "SiteTouristiqueJournees",
                        principalColumn: "IdSiteTouristiqueJournee",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiqueGlobalQuotas",
                columns: table => new
                {
                    IdSiteTouristiqueJournee = table.Column<int>(type: "int", nullable: false),
                    CapaciteTotale = table.Column<int>(type: "int", nullable: false),
                    QuantiteHold = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    QuantiteVendue = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiqueGlobalQuotas", x => x.IdSiteTouristiqueJournee);
                    table.CheckConstraint("CK_SiteTouristiqueGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
                    table.CheckConstraint("CK_SiteTouristiqueGlobalQuotas_StockMax", "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
                    table.CheckConstraint("CK_SiteTouristiqueGlobalQuotas_StockPositive", "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueGlobalQuotas_SiteTouristiqueJournees_IdSiteTo~",
                        column: x => x.IdSiteTouristiqueJournee,
                        principalTable: "SiteTouristiqueJournees",
                        principalColumn: "IdSiteTouristiqueJournee",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiqueReservations",
                columns: table => new
                {
                    IdSiteTouristiqueReservation = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSiteTouristiqueJournee = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    ReferenceReservation = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerRef = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: true),
                    IdClient = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "enum('HOLD','CONFIRMED','CANCELLED','EXPIRED')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MontantSousTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiqueReservations", x => x.IdSiteTouristiqueReservation);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueReservations_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueReservations_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueReservations_SiteTouristiqueJournees_IdSiteTo~",
                        column: x => x.IdSiteTouristiqueJournee,
                        principalTable: "SiteTouristiqueJournees",
                        principalColumn: "IdSiteTouristiqueJournee",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueReservations_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantCreneauGlobalQuotas",
                columns: table => new
                {
                    IdRestaurantCreneau = table.Column<int>(type: "int", nullable: false),
                    CapaciteTotale = table.Column<int>(type: "int", nullable: false),
                    QuantiteHold = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    QuantiteVendue = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantCreneauGlobalQuotas", x => x.IdRestaurantCreneau);
                    table.CheckConstraint("CK_RestaurantCreneauGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
                    table.CheckConstraint("CK_RestaurantCreneauGlobalQuotas_StockMax", "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
                    table.CheckConstraint("CK_RestaurantCreneauGlobalQuotas_StockPositive", "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                    table.ForeignKey(
                        name: "FK_RestaurantCreneauGlobalQuotas_RestaurantCreneaux_IdRestauran~",
                        column: x => x.IdRestaurantCreneau,
                        principalTable: "RestaurantCreneaux",
                        principalColumn: "IdRestaurantCreneau",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantCreneauZoneQuotas",
                columns: table => new
                {
                    IdRestaurantCreneauZoneQuota = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdRestaurantCreneau = table.Column<int>(type: "int", nullable: false),
                    IdRestaurantZone = table.Column<int>(type: "int", nullable: false),
                    CapaciteTotale = table.Column<int>(type: "int", nullable: false),
                    QuantiteHold = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    QuantiteVendue = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantCreneauZoneQuotas", x => x.IdRestaurantCreneauZoneQuota);
                    table.CheckConstraint("CK_RestaurantCreneauZoneQuotas_Capacite", "`CapaciteTotale` >= 0");
                    table.CheckConstraint("CK_RestaurantCreneauZoneQuotas_StockMax", "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
                    table.CheckConstraint("CK_RestaurantCreneauZoneQuotas_StockPositive", "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                    table.ForeignKey(
                        name: "FK_RestaurantCreneauZoneQuotas_RestaurantCreneaux_IdRestaurantC~",
                        column: x => x.IdRestaurantCreneau,
                        principalTable: "RestaurantCreneaux",
                        principalColumn: "IdRestaurantCreneau",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantCreneauZoneQuotas_RestaurantZones_IdRestaurantZone",
                        column: x => x.IdRestaurantZone,
                        principalTable: "RestaurantZones",
                        principalColumn: "IdRestaurantZone",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantReservations",
                columns: table => new
                {
                    IdRestaurantReservation = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdRestaurant = table.Column<int>(type: "int", nullable: false),
                    IdRestaurantCreneau = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: true),
                    IdClient = table.Column<int>(type: "int", nullable: true),
                    ReferenceReservation = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerRef = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "enum('HOLD','CONFIRMED','CANCELLED','EXPIRED')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MontantSousTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreCouverts = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantReservations", x => x.IdRestaurantReservation);
                    table.ForeignKey(
                        name: "FK_RestaurantReservations_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantReservations_RestaurantCreneaux_IdRestaurantCreneau",
                        column: x => x.IdRestaurantCreneau,
                        principalTable: "RestaurantCreneaux",
                        principalColumn: "IdRestaurantCreneau",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantReservations_Restaurants_IdRestaurant",
                        column: x => x.IdRestaurant,
                        principalTable: "Restaurants",
                        principalColumn: "IdRestaurant",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantReservations_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantReservations_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiquePayments",
                columns: table => new
                {
                    IdSiteTouristiquePayment = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSiteTouristiqueReservation = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    ReferencePaiement = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Provider = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderTxRef = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "enum('PENDING','SUCCEEDED','FAILED','REFUNDED')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantTarif = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDeviseTarif = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TauxVersDevisePaiement = table.Column<decimal>(type: "decimal(18,8)", nullable: false, defaultValue: 1m),
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiquePayments", x => x.IdSiteTouristiquePayment);
                    table.ForeignKey(
                        name: "FK_SiteTouristiquePayments_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiquePayments_SiteTouristiqueReservations_IdSiteTo~",
                        column: x => x.IdSiteTouristiqueReservation,
                        principalTable: "SiteTouristiqueReservations",
                        principalColumn: "IdSiteTouristiqueReservation",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiqueReservationLines",
                columns: table => new
                {
                    IdSiteTouristiqueReservationLine = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSiteTouristiqueReservation = table.Column<int>(type: "int", nullable: false),
                    LineType = table.Column<string>(type: "enum('ClassQuota','GlobalQuota')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdSiteTouristiqueClassQuota = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiqueReservationLines", x => x.IdSiteTouristiqueReservationLine);
                    table.CheckConstraint("CK_SiteTouristiqueReservationLines_Quantite", "`Quantite` > 0");
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueReservationLines_SiteTouristiqueClassQuotas_I~",
                        column: x => x.IdSiteTouristiqueClassQuota,
                        principalTable: "SiteTouristiqueClassQuotas",
                        principalColumn: "IdSiteTouristiqueClassQuota",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueReservationLines_SiteTouristiqueReservations_~",
                        column: x => x.IdSiteTouristiqueReservation,
                        principalTable: "SiteTouristiqueReservations",
                        principalColumn: "IdSiteTouristiqueReservation",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantPayments",
                columns: table => new
                {
                    IdRestaurantPayment = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdRestaurantReservation = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    ReferencePaiement = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Provider = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderTxRef = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "enum('PENDING','SUCCEEDED','FAILED','REFUNDED')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantTarif = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDeviseTarif = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TauxVersDevisePaiement = table.Column<decimal>(type: "decimal(18,8)", nullable: false, defaultValue: 1m),
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantPayments", x => x.IdRestaurantPayment);
                    table.ForeignKey(
                        name: "FK_RestaurantPayments_RestaurantReservations_IdRestaurantReserv~",
                        column: x => x.IdRestaurantReservation,
                        principalTable: "RestaurantReservations",
                        principalColumn: "IdRestaurantReservation",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantPayments_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantReservationLines",
                columns: table => new
                {
                    IdRestaurantReservationLine = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdRestaurantReservation = table.Column<int>(type: "int", nullable: false),
                    LineType = table.Column<string>(type: "enum('GlobalQuota','ClassQuota')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontantLigne = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdRestaurantCreneauGlobalQuota = table.Column<int>(type: "int", nullable: true),
                    IdRestaurantCreneauZoneQuota = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantReservationLines", x => x.IdRestaurantReservationLine);
                    table.CheckConstraint("CK_RestaurantReservationLines_Quantite", "`Quantite` > 0");
                    table.ForeignKey(
                        name: "FK_RestaurantReservationLines_RestaurantCreneauGlobalQuotas_IdR~",
                        column: x => x.IdRestaurantCreneauGlobalQuota,
                        principalTable: "RestaurantCreneauGlobalQuotas",
                        principalColumn: "IdRestaurantCreneau",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantReservationLines_RestaurantCreneauZoneQuotas_IdRes~",
                        column: x => x.IdRestaurantCreneauZoneQuota,
                        principalTable: "RestaurantCreneauZoneQuotas",
                        principalColumn: "IdRestaurantCreneauZoneQuota",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantReservationLines_RestaurantReservations_IdRestaura~",
                        column: x => x.IdRestaurantReservation,
                        principalTable: "RestaurantReservations",
                        principalColumn: "IdRestaurantReservation",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiqueTickets",
                columns: table => new
                {
                    IdSiteTouristiqueTicket = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSiteTouristiqueReservationLine = table.Column<int>(type: "int", nullable: false),
                    TicketCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "enum('ISSUED','USED','VOID')", nullable: false, defaultValue: "ISSUED")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiqueTickets", x => x.IdSiteTouristiqueTicket);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueTickets_SiteTouristiqueReservationLines_IdSit~",
                        column: x => x.IdSiteTouristiqueReservationLine,
                        principalTable: "SiteTouristiqueReservationLines",
                        principalColumn: "IdSiteTouristiqueReservationLine",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCreneaux_IdRestaurant",
                table: "RestaurantCreneaux",
                column: "IdRestaurant");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCreneaux_IdRestaurant_StartAtUtc",
                table: "RestaurantCreneaux",
                columns: new[] { "IdRestaurant", "StartAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCreneaux_IdRestaurantPlanification",
                table: "RestaurantCreneaux",
                column: "IdRestaurantPlanification");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCreneaux_IdRestaurantPlanificationPlage",
                table: "RestaurantCreneaux",
                column: "IdRestaurantPlanificationPlage");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCreneaux_IdSociete_DateService",
                table: "RestaurantCreneaux",
                columns: new[] { "IdSociete", "DateService" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCreneauZoneQuotas_Creneau_Zone_UQ",
                table: "RestaurantCreneauZoneQuotas",
                columns: new[] { "IdRestaurantCreneau", "IdRestaurantZone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCreneauZoneQuotas_IdRestaurantCreneau",
                table: "RestaurantCreneauZoneQuotas",
                column: "IdRestaurantCreneau");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCreneauZoneQuotas_IdRestaurantZone",
                table: "RestaurantCreneauZoneQuotas",
                column: "IdRestaurantZone");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPayments_Idempotency_UQ",
                table: "RestaurantPayments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPayments_IdSite",
                table: "RestaurantPayments",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPayments_ReferencePaiement_UQ",
                table: "RestaurantPayments",
                column: "ReferencePaiement",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPayments_Reservation_Status",
                table: "RestaurantPayments",
                columns: new[] { "IdRestaurantReservation", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPlanifGenerationLogs_IdPlanification",
                table: "RestaurantPlanifGenerationLogs",
                column: "IdRestaurantPlanification");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPlanificationPlages_IdPlanification",
                table: "RestaurantPlanificationPlages",
                column: "IdRestaurantPlanification");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPlanifications_IdRestaurant",
                table: "RestaurantPlanifications",
                column: "IdRestaurant");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPlanifications_IdSociete",
                table: "RestaurantPlanifications",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPlanifPlageZoneQuotas_IdPlage",
                table: "RestaurantPlanifPlageZoneQuotas",
                column: "IdRestaurantPlanificationPlage");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPlanifPlageZoneQuotas_IdRestaurantZone",
                table: "RestaurantPlanifPlageZoneQuotas",
                column: "IdRestaurantZone");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPlanifPlageZoneQuotas_Plage_Zone_UQ",
                table: "RestaurantPlanifPlageZoneQuotas",
                columns: new[] { "IdRestaurantPlanificationPlage", "IdRestaurantZone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservationLines_IdReservation",
                table: "RestaurantReservationLines",
                column: "IdRestaurantReservation");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservationLines_IdRestaurantCreneauGlobalQuota",
                table: "RestaurantReservationLines",
                column: "IdRestaurantCreneauGlobalQuota");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservationLines_IdZoneQuota",
                table: "RestaurantReservationLines",
                column: "IdRestaurantCreneauZoneQuota");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservations_Creneau_Status",
                table: "RestaurantReservations",
                columns: new[] { "IdRestaurantCreneau", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservations_IdClient",
                table: "RestaurantReservations",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservations_IdRestaurant",
                table: "RestaurantReservations",
                column: "IdRestaurant");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservations_IdSite",
                table: "RestaurantReservations",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservations_IdUtilisateur",
                table: "RestaurantReservations",
                column: "IdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservations_Societe_Idempotency_UQ",
                table: "RestaurantReservations",
                columns: new[] { "IdSociete", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservations_Societe_Reference_UQ",
                table: "RestaurantReservations",
                columns: new[] { "IdSociete", "ReferenceReservation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantReservations_Status_ExpiresAtUtc",
                table: "RestaurantReservations",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_IdSite",
                table: "Restaurants",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_Societe_CodeRestaurant_UQ",
                table: "Restaurants",
                columns: new[] { "IdSociete", "CodeRestaurant" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantZones_IdRestaurant",
                table: "RestaurantZones",
                column: "IdRestaurant");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantZones_IdSociete",
                table: "RestaurantZones",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantZones_Restaurant_Code_UQ",
                table: "RestaurantZones",
                columns: new[] { "IdRestaurant", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueClasses_IdSociete",
                table: "SiteTouristiqueClasses",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueClasses_Societe_Code_UQ",
                table: "SiteTouristiqueClasses",
                columns: new[] { "IdSociete", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueClassQuotas_IdSiteTouristiqueClasse",
                table: "SiteTouristiqueClassQuotas",
                column: "IdSiteTouristiqueClasse");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueClassQuotas_IdSiteTouristiqueJournee",
                table: "SiteTouristiqueClassQuotas",
                column: "IdSiteTouristiqueJournee");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueClassQuotas_Journee_Classe_UQ",
                table: "SiteTouristiqueClassQuotas",
                columns: new[] { "IdSiteTouristiqueJournee", "IdSiteTouristiqueClasse" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueJournees_IdSiteTouristique",
                table: "SiteTouristiqueJournees",
                column: "IdSiteTouristique");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueJournees_IdSiteTouristiquePlanification",
                table: "SiteTouristiqueJournees",
                column: "IdSiteTouristiquePlanification");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueJournees_IdSociete_DateVisite",
                table: "SiteTouristiqueJournees",
                columns: new[] { "IdSociete", "DateVisite" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueJournees_Lieu_DateVisite_UQ",
                table: "SiteTouristiqueJournees",
                columns: new[] { "IdSiteTouristique", "DateVisite" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePayments_Idempotency_UQ",
                table: "SiteTouristiquePayments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePayments_IdSite",
                table: "SiteTouristiquePayments",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePayments_ReferencePaiement_UQ",
                table: "SiteTouristiquePayments",
                column: "ReferencePaiement",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePayments_Reservation_Status",
                table: "SiteTouristiquePayments",
                columns: new[] { "IdSiteTouristiqueReservation", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePlanifClassQuotas_IdPlanification",
                table: "SiteTouristiquePlanifClassQuotas",
                column: "IdSiteTouristiquePlanification");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePlanifClassQuotas_IdSiteTouristiqueClasse",
                table: "SiteTouristiquePlanifClassQuotas",
                column: "IdSiteTouristiqueClasse");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePlanifClassQuotas_Planif_Classe_UQ",
                table: "SiteTouristiquePlanifClassQuotas",
                columns: new[] { "IdSiteTouristiquePlanification", "IdSiteTouristiqueClasse" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePlanifGenerationLogs_IdPlanification",
                table: "SiteTouristiquePlanifGenerationLogs",
                column: "IdSiteTouristiquePlanification");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePlanifications_IdSiteTouristique",
                table: "SiteTouristiquePlanifications",
                column: "IdSiteTouristique");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePlanifications_IdSociete",
                table: "SiteTouristiquePlanifications",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueReservationLines_IdReservation",
                table: "SiteTouristiqueReservationLines",
                column: "IdSiteTouristiqueReservation");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueReservationLines_IdSiteTouristiqueClassQuota",
                table: "SiteTouristiqueReservationLines",
                column: "IdSiteTouristiqueClassQuota");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueReservations_IdClient",
                table: "SiteTouristiqueReservations",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueReservations_IdSite",
                table: "SiteTouristiqueReservations",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueReservations_IdUtilisateur",
                table: "SiteTouristiqueReservations",
                column: "IdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueReservations_Journee_Status",
                table: "SiteTouristiqueReservations",
                columns: new[] { "IdSiteTouristiqueJournee", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueReservations_Societe_Idempotency_UQ",
                table: "SiteTouristiqueReservations",
                columns: new[] { "IdSociete", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueReservations_Societe_Reference_UQ",
                table: "SiteTouristiqueReservations",
                columns: new[] { "IdSociete", "ReferenceReservation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueReservations_Status_ExpiresAtUtc",
                table: "SiteTouristiqueReservations",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiques_IdSite",
                table: "SiteTouristiques",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiques_Societe_CodeLieu_UQ",
                table: "SiteTouristiques",
                columns: new[] { "IdSociete", "CodeLieu" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueTickets_IdSiteTouristiqueReservationLine",
                table: "SiteTouristiqueTickets",
                column: "IdSiteTouristiqueReservationLine");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueTickets_Status",
                table: "SiteTouristiqueTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueTickets_TicketCode_UQ",
                table: "SiteTouristiqueTickets",
                column: "TicketCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantPayments");

            migrationBuilder.DropTable(
                name: "RestaurantPlanifGenerationLogs");

            migrationBuilder.DropTable(
                name: "RestaurantPlanifPlageGlobalQuotas");

            migrationBuilder.DropTable(
                name: "RestaurantPlanifPlageZoneQuotas");

            migrationBuilder.DropTable(
                name: "RestaurantReservationLines");

            migrationBuilder.DropTable(
                name: "SiteTouristiqueGlobalQuotas");

            migrationBuilder.DropTable(
                name: "SiteTouristiquePayments");

            migrationBuilder.DropTable(
                name: "SiteTouristiquePlanifClassQuotas");

            migrationBuilder.DropTable(
                name: "SiteTouristiquePlanifGenerationLogs");

            migrationBuilder.DropTable(
                name: "SiteTouristiquePlanifGlobalQuotas");

            migrationBuilder.DropTable(
                name: "SiteTouristiqueTickets");

            migrationBuilder.DropTable(
                name: "RestaurantCreneauGlobalQuotas");

            migrationBuilder.DropTable(
                name: "RestaurantCreneauZoneQuotas");

            migrationBuilder.DropTable(
                name: "RestaurantReservations");

            migrationBuilder.DropTable(
                name: "SiteTouristiqueReservationLines");

            migrationBuilder.DropTable(
                name: "RestaurantZones");

            migrationBuilder.DropTable(
                name: "RestaurantCreneaux");

            migrationBuilder.DropTable(
                name: "SiteTouristiqueClassQuotas");

            migrationBuilder.DropTable(
                name: "SiteTouristiqueReservations");

            migrationBuilder.DropTable(
                name: "RestaurantPlanificationPlages");

            migrationBuilder.DropTable(
                name: "SiteTouristiqueClasses");

            migrationBuilder.DropTable(
                name: "SiteTouristiqueJournees");

            migrationBuilder.DropTable(
                name: "RestaurantPlanifications");

            migrationBuilder.DropTable(
                name: "SiteTouristiquePlanifications");

            migrationBuilder.DropTable(
                name: "Restaurants");

            migrationBuilder.DropTable(
                name: "SiteTouristiques");

            migrationBuilder.DropColumn(
                name: "DureeHoldRestaurantMinutes",
                table: "ConfigSocietes");

            migrationBuilder.DropColumn(
                name: "DureeHoldSiteTouristiqueMinutes",
                table: "ConfigSocietes");
        }
    }
}
