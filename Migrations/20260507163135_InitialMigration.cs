using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    IdAudit = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TableName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecordId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserRole = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdSociete = table.Column<int>(type: "int", nullable: true),
                    DateAction = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OldValues = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewValues = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedFields = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Commentaire = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HttpMethod = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Endpoint = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    Success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.IdAudit);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    IdClient = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NomClient = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdresseClient = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telephone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailClient = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GenreClient = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    IsActif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Province = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ville = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Commune = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Avenue = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Numero = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.IdClient);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    IdPermission = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Categorie = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.IdPermission);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    IdRole = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nom = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Niveau = table.Column<int>(type: "int", nullable: true),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.IdRole);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Societes",
                columns: table => new
                {
                    IdSociete = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nom = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Devise = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Logo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telephone = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailContact = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SiteWeb = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomCompletResponsable = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GenreResponsable = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    AdresseResidence = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Societes", x => x.IdSociete);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    IdRolePermission = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdRole = table.Column<int>(type: "int", nullable: false),
                    IdPermission = table.Column<int>(type: "int", nullable: false),
                    DateAttribution = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IdUtilisateurAttribution = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.IdRolePermission);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_IdPermission",
                        column: x => x.IdPermission,
                        principalTable: "Permissions",
                        principalColumn: "IdPermission",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_IdRole",
                        column: x => x.IdRole,
                        principalTable: "Roles",
                        principalColumn: "IdRole",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CategorieSieges",
                columns: table => new
                {
                    IdCategorieSiege = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    CodeCategorieSiege = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Libelle = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorieSieges", x => x.IdCategorieSiege);
                    table.ForeignKey(
                        name: "FK_CategorieSieges_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    IdDestination = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VilleDepart = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VilleArrivee = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HeureDepart = table.Column<TimeOnly>(type: "time", nullable: true),
                    jourDepart = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IdSociete = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.IdDestination);
                    table.ForeignKey(
                        name: "FK_Destinations_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    IdSite = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    CodeSite = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomSite = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ville = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Adresse = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telephone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomResponsableSite = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Genre = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.IdSite);
                    table.ForeignKey(
                        name: "FK_Sites_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TypeVehicules",
                columns: table => new
                {
                    IdTypeVehicule = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Libelle = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeVehicules", x => x.IdTypeVehicule);
                    table.ForeignKey(
                        name: "FK_TypeVehicules_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    IdAgent = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Matricule = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomComplet = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Genre = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateNaissance = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TelephoneAgent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailAgent = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    EtatCivil = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fonction = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RoleAgent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhotoUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    AdresseResidence = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Zone = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.IdAgent);
                    table.ForeignKey(
                        name: "FK_Agents_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Agents_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Vehicules",
                columns: table => new
                {
                    IdVehicule = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Marques = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AliasVehicule = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdTypeVehicule = table.Column<int>(type: "int", nullable: false),
                    NombreSiege = table.Column<int>(type: "int", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    NumeroDePlaque = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Photo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicules", x => x.IdVehicule);
                    table.ForeignKey(
                        name: "FK_Vehicules_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicules_TypeVehicules_IdTypeVehicule",
                        column: x => x.IdTypeVehicule,
                        principalTable: "TypeVehicules",
                        principalColumn: "IdTypeVehicule",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReferenceUtilisateur = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    NomComplet = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telephone = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhotoUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LieuNaissance = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateNaissance = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Genre = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MotDePasseHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultUsername = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DoitChangerMotDePasse = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    IdRole = table.Column<int>(type: "int", nullable: true),
                    IdSociete = table.Column<int>(type: "int", nullable: true),
                    AdresseResidence = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsConnecte = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IdAgent = table.Column<int>(type: "int", nullable: true),
                    IdClient = table.Column<int>(type: "int", nullable: true),
                    IdSite = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.IdUtilisateur);
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Agents_IdAgent",
                        column: x => x.IdAgent,
                        principalTable: "Agents",
                        principalColumn: "IdAgent");
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "IdClient");
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Roles_IdRole",
                        column: x => x.IdRole,
                        principalTable: "Roles",
                        principalColumn: "IdRole",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sieges",
                columns: table => new
                {
                    IdSiege = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdVehicule = table.Column<int>(type: "int", nullable: false),
                    NumeroOrdre = table.Column<int>(type: "int", nullable: false),
                    CodeSiege = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstActif = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdCategorieSiege = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sieges", x => x.IdSiege);
                    table.ForeignKey(
                        name: "FK_Sieges_CategorieSieges_IdCategorieSiege",
                        column: x => x.IdCategorieSiege,
                        principalTable: "CategorieSieges",
                        principalColumn: "IdCategorieSiege",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sieges_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sieges_Vehicules_IdVehicule",
                        column: x => x.IdVehicule,
                        principalTable: "Vehicules",
                        principalColumn: "IdVehicule",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Voyages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    date_depart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    heure_depart = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    prix = table.Column<int>(type: "int", nullable: false),
                    IdVehicule = table.Column<int>(type: "int", nullable: false),
                    IdDestination = table.Column<int>(type: "int", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voyages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Voyages_Destinations_IdDestination",
                        column: x => x.IdDestination,
                        principalTable: "Destinations",
                        principalColumn: "IdDestination",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Voyages_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Voyages_Vehicules_IdVehicule",
                        column: x => x.IdVehicule,
                        principalTable: "Vehicules",
                        principalColumn: "IdVehicule",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommunicationCampaigns",
                columns: table => new
                {
                    IdCampagne = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Titre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Contenu = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypeCampagne = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdSociete = table.Column<int>(type: "int", nullable: true),
                    IdUtilisateurCreateur = table.Column<int>(type: "int", nullable: false),
                    CriteresCiblage = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ListeIdClients = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActiverPush = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ActiverSms = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ActiverEmail = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ActiverInApp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateEnvoi = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EstProgrammee = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EstEnCours = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EstTerminee = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NombreDestinataires = table.Column<int>(type: "int", nullable: false),
                    NombreEnvoyes = table.Column<int>(type: "int", nullable: false),
                    NombreSucces = table.Column<int>(type: "int", nullable: false),
                    NombreEchecs = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateDerniereModification = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateEnvoiEffectif = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationCampaigns", x => x.IdCampagne);
                    table.ForeignKey(
                        name: "FK_CommunicationCampaigns_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CommunicationCampaigns_Utilisateurs_IdUtilisateurCreateur",
                        column: x => x.IdUtilisateurCreateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    IdNotificationPreference = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    AllowPush = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowInApp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowSms = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowEmail = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OptOutGlobal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OptOutFactures = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.IdNotificationPreference);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Utilisateurs_IdUtilisateur",
                        column: x => x.IdUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    IdNotification = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Titre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Contenu = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypeNotification = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstLue = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateLecture = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LienAction = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IdExpediteur = table.Column<int>(type: "int", nullable: true),
                    IdDestinataire = table.Column<int>(type: "int", nullable: true),
                    IdSociete = table.Column<int>(type: "int", nullable: true),
                    IdAgent = table.Column<int>(type: "int", nullable: true),
                    CanalUtilise = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priorite = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatutEnvoi = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrackingId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.IdNotification);
                    table.ForeignKey(
                        name: "FK_Notifications_Agents_IdAgent",
                        column: x => x.IdAgent,
                        principalTable: "Agents",
                        principalColumn: "IdAgent");
                    table.ForeignKey(
                        name: "FK_Notifications_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete");
                    table.ForeignKey(
                        name: "FK_Notifications_Utilisateurs_IdDestinataire",
                        column: x => x.IdDestinataire,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur");
                    table.ForeignKey(
                        name: "FK_Notifications_Utilisateurs_IdExpediteur",
                        column: x => x.IdExpediteur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    IdPasswordResetToken = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUtilisation = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.IdPasswordResetToken);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Utilisateurs_IdUtilisateur",
                        column: x => x.IdUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlainteClients",
                columns: table => new
                {
                    IdPlainte = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    Titre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypePanne = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NiveauImportance = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RisquesPrincipaux = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatutPlainte = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priorite = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdAgentAssigné = table.Column<int>(type: "int", nullable: true),
                    IdUtilisateurCreateur = table.Column<int>(type: "int", nullable: true),
                    CommentaireResolution = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateResolution = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EstUrgente = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateDerniereModification = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlainteClients", x => x.IdPlainte);
                    table.ForeignKey(
                        name: "FK_PlainteClients_Agents_IdAgentAssigné",
                        column: x => x.IdAgentAssigné,
                        principalTable: "Agents",
                        principalColumn: "IdAgent",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlainteClients_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlainteClients_Utilisateurs_IdUtilisateurCreateur",
                        column: x => x.IdUtilisateurCreateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    IdRefreshToken = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateRevocation = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeviceInfo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.IdRefreshToken);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Utilisateurs_IdUtilisateur",
                        column: x => x.IdUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SmsLogs",
                columns: table => new
                {
                    IdSmsLog = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroDestinataire = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "varchar(1600)", maxLength: 1600, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypeNotification = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageSid = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageErreur = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeErreur = table.Column<int>(type: "int", nullable: true),
                    CoutUsd = table.Column<double>(type: "double", nullable: false),
                    CoutFc = table.Column<double>(type: "double", nullable: false),
                    DateEnvoi = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateLivraison = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateEchec = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NombreSegments = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroExpediteur = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UtilisateurIdUtilisateur = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsLogs", x => x.IdSmsLog);
                    table.ForeignKey(
                        name: "FK_SmsLogs_Utilisateurs_UtilisateurIdUtilisateur",
                        column: x => x.UtilisateurIdUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserDevices",
                columns: table => new
                {
                    IdUserDevice = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    FcmToken = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceModel = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OsVersion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultDevice = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    DateEnregistrement = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateDerniereUtilisation = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.IdUserDevice);
                    table.ForeignKey(
                        name: "FK_UserDevices_Utilisateurs_IdUtilisateur",
                        column: x => x.IdUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    IdUserPermission = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    IdPermission = table.Column<int>(type: "int", nullable: false),
                    IsGranted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateAttribution = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Commentaire = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttribueParIdUtilisateur = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.IdUserPermission);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_IdPermission",
                        column: x => x.IdPermission,
                        principalTable: "Permissions",
                        principalColumn: "IdPermission",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Utilisateurs_AttribueParIdUtilisateur",
                        column: x => x.AttribueParIdUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur");
                    table.ForeignKey(
                        name: "FK_UserPermissions_Utilisateurs_IdUtilisateur",
                        column: x => x.IdUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    IdUserRole = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    IdRole = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateAttribution = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IdUtilisateurAttribution = table.Column<int>(type: "int", nullable: true),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.IdUserRole);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_IdRole",
                        column: x => x.IdRole,
                        principalTable: "Roles",
                        principalColumn: "IdRole",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Utilisateurs_IdUtilisateur",
                        column: x => x.IdUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    IdReservation = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    IdVoyage = table.Column<int>(type: "int", nullable: false),
                    StatutReservation = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    dateReservation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    nombreDePlace = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.IdReservation);
                    table.ForeignKey(
                        name: "FK_Reservations_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Utilisateurs_IdUtilisateur",
                        column: x => x.IdUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Voyages_IdVoyage",
                        column: x => x.IdVoyage,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VoyageDestinations",
                columns: table => new
                {
                    IdVoyageDestination = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdVoyage = table.Column<int>(type: "int", nullable: false),
                    IdDestination = table.Column<int>(type: "int", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoyageDestinations", x => x.IdVoyageDestination);
                    table.ForeignKey(
                        name: "FK_VoyageDestinations_Destinations_IdDestination",
                        column: x => x.IdDestination,
                        principalTable: "Destinations",
                        principalColumn: "IdDestination",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoyageDestinations_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoyageDestinations_Voyages_IdVoyage",
                        column: x => x.IdVoyage,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VoyageTarifsCategorieSiege",
                columns: table => new
                {
                    IdVoyageTarifCategorieSiege = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdVoyage = table.Column<int>(type: "int", nullable: false),
                    IdCategorieSiege = table.Column<int>(type: "int", nullable: false),
                    Prix = table.Column<int>(type: "int", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoyageTarifsCategorieSiege", x => x.IdVoyageTarifCategorieSiege);
                    table.ForeignKey(
                        name: "FK_VoyageTarifsCategorieSiege_CategorieSieges_IdCategorieSiege",
                        column: x => x.IdCategorieSiege,
                        principalTable: "CategorieSieges",
                        principalColumn: "IdCategorieSiege",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoyageTarifsCategorieSiege_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoyageTarifsCategorieSiege_Voyages_IdVoyage",
                        column: x => x.IdVoyage,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReservationPassengers",
                columns: table => new
                {
                    IdReservationPassenger = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdReservation = table.Column<int>(type: "int", nullable: false),
                    IdClient = table.Column<int>(type: "int", nullable: true),
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
                    DateNaissance = table.Column<DateTime>(type: "date", nullable: true),
                    Genre = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationPassengers", x => x.IdReservationPassenger);
                    table.ForeignKey(
                        name: "FK_ReservationPassengers_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationPassengers_Reservations_IdReservation",
                        column: x => x.IdReservation,
                        principalTable: "Reservations",
                        principalColumn: "IdReservation",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservationPassengers_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Billets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IsUsed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IdReservation = table.Column<int>(type: "int", nullable: true),
                    QrCode = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dateGeneration = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    IdClient = table.Column<int>(type: "int", nullable: true),
                    IdReservationPassenger = table.Column<int>(type: "int", nullable: true),
                    IdSiege = table.Column<int>(type: "int", nullable: true),
                    CodeSiege = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Billets_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Billets_ReservationPassengers_IdReservationPassenger",
                        column: x => x.IdReservationPassenger,
                        principalTable: "ReservationPassengers",
                        principalColumn: "IdReservationPassenger",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Billets_Reservations_IdReservation",
                        column: x => x.IdReservation,
                        principalTable: "Reservations",
                        principalColumn: "IdReservation",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Billets_Sieges_IdSiege",
                        column: x => x.IdSiege,
                        principalTable: "Sieges",
                        principalColumn: "IdSiege",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Billets_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Billets_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VoyageSeatAllocations",
                columns: table => new
                {
                    IdVoyageSeatAllocation = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdVoyage = table.Column<int>(type: "int", nullable: false),
                    IdSiege = table.Column<int>(type: "int", nullable: false),
                    IdReservationPassenger = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoyageSeatAllocations", x => x.IdVoyageSeatAllocation);
                    table.ForeignKey(
                        name: "FK_VoyageSeatAllocations_ReservationPassengers_IdReservationPas~",
                        column: x => x.IdReservationPassenger,
                        principalTable: "ReservationPassengers",
                        principalColumn: "IdReservationPassenger",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoyageSeatAllocations_Sieges_IdSiege",
                        column: x => x.IdSiege,
                        principalTable: "Sieges",
                        principalColumn: "IdSiege",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoyageSeatAllocations_Voyages_IdVoyage",
                        column: x => x.IdVoyage,
                        principalTable: "Voyages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BilletEmbarquements",
                columns: table => new
                {
                    IdEmbarquement = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdBillet = table.Column<int>(type: "int", nullable: false),
                    IdReservationPassenger = table.Column<int>(type: "int", nullable: false),
                    DateEmbarquementUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IdUtilisateurEnregistrement = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BilletEmbarquements", x => x.IdEmbarquement);
                    table.ForeignKey(
                        name: "FK_BilletEmbarquements_Billets_IdBillet",
                        column: x => x.IdBillet,
                        principalTable: "Billets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BilletEmbarquements_ReservationPassengers_IdReservationPasse~",
                        column: x => x.IdReservationPassenger,
                        principalTable: "ReservationPassengers",
                        principalColumn: "IdReservationPassenger",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BilletEmbarquements_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BilletEmbarquements_Utilisateurs_IdUtilisateurEnregistrement",
                        column: x => x.IdUtilisateurEnregistrement,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Paiements",
                columns: table => new
                {
                    IdPaiement = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MontantAPaye = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontantPaye = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ResteAPaye = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MethodePaiement = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceTransaction = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    IdReservation = table.Column<int>(type: "int", nullable: true),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    DateEmissionBillet = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IdBilletEmis = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paiements", x => x.IdPaiement);
                    table.ForeignKey(
                        name: "FK_Paiements_Billets_IdBilletEmis",
                        column: x => x.IdBilletEmis,
                        principalTable: "Billets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Paiements_Reservations_IdReservation",
                        column: x => x.IdReservation,
                        principalTable: "Reservations",
                        principalColumn: "IdReservation",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Paiements_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Paiements_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Paiements_Utilisateurs_IdUtilisateur",
                        column: x => x.IdUtilisateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Email_Unique",
                table: "Agents",
                column: "EmailAgent",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_IdSite",
                table: "Agents",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_IdSociete",
                table: "Agents",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Matricule_Unique",
                table: "Agents",
                column: "Matricule",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_SerialNumber_Unique",
                table: "Agents",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_DateAction",
                table: "AuditLogs",
                column: "DateAction");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_IdSociete",
                table: "AuditLogs",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Table_Record",
                table: "AuditLogs",
                columns: new[] { "TableName", "RecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BilletEmbarquements_IdBillet_Unique",
                table: "BilletEmbarquements",
                column: "IdBillet",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BilletEmbarquements_IdReservationPassenger",
                table: "BilletEmbarquements",
                column: "IdReservationPassenger");

            migrationBuilder.CreateIndex(
                name: "IX_BilletEmbarquements_IdSociete",
                table: "BilletEmbarquements",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_BilletEmbarquements_IdUtilisateurEnregistrement",
                table: "BilletEmbarquements",
                column: "IdUtilisateurEnregistrement");

            migrationBuilder.CreateIndex(
                name: "IX_Billets_DateGeneration",
                table: "Billets",
                column: "dateGeneration");

            migrationBuilder.CreateIndex(
                name: "IX_Billets_IdClient",
                table: "Billets",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_Billets_IdReservation",
                table: "Billets",
                column: "IdReservation");

            migrationBuilder.CreateIndex(
                name: "IX_Billets_IdReservationPassenger",
                table: "Billets",
                column: "IdReservationPassenger");

            migrationBuilder.CreateIndex(
                name: "IX_Billets_IdSiege",
                table: "Billets",
                column: "IdSiege");

            migrationBuilder.CreateIndex(
                name: "IX_Billets_IdSite",
                table: "Billets",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_Billets_IdSociete",
                table: "Billets",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Billets_QrCode",
                table: "Billets",
                column: "QrCode");

            migrationBuilder.CreateIndex(
                name: "IX_CategorieSieges_IdSociete",
                table: "CategorieSieges",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_CategorieSieges_Societe_Code_Unique",
                table: "CategorieSieges",
                columns: new[] { "IdSociete", "CodeCategorieSiege" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_EmailClient_Unique",
                table: "Clients",
                column: "EmailClient",
                unique: true,
                filter: "EmailClient IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Sync",
                table: "Clients",
                columns: new[] { "UpdatedAt", "IdClient" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Telephone_Unique",
                table: "Clients",
                column: "Telephone",
                unique: true,
                filter: "Telephone IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationCampaigns_IdSociete",
                table: "CommunicationCampaigns",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationCampaigns_IdUtilisateurCreateur",
                table: "CommunicationCampaigns",
                column: "IdUtilisateurCreateur");

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_IdSociete",
                table: "Destinations",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_Villes",
                table: "Destinations",
                columns: new[] { "VilleDepart", "VilleArrivee" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_IdUtilisateur",
                table: "NotificationPreferences",
                column: "IdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdAgent",
                table: "Notifications",
                column: "IdAgent");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdDestinataire",
                table: "Notifications",
                column: "IdDestinataire");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdExpediteur",
                table: "Notifications",
                column: "IdExpediteur");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdSociete",
                table: "Notifications",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_DateCreation",
                table: "Paiements",
                column: "DateCreation");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_IdBilletEmis",
                table: "Paiements",
                column: "IdBilletEmis");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_IdReservation",
                table: "Paiements",
                column: "IdReservation");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_IdSite",
                table: "Paiements",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_IdSociete",
                table: "Paiements",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_IdUtilisateur",
                table: "Paiements",
                column: "IdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_Statut",
                table: "Paiements",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_IdUtilisateur",
                table: "PasswordResetTokens",
                column: "IdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Token",
                table: "PasswordResetTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlainteClients_IdAgentAssigné",
                table: "PlainteClients",
                column: "IdAgentAssigné");

            migrationBuilder.CreateIndex(
                name: "IX_PlainteClients_IdClient",
                table: "PlainteClients",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_PlainteClients_IdUtilisateurCreateur",
                table: "PlainteClients",
                column: "IdUtilisateurCreateur");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IdUtilisateur",
                table: "RefreshTokens",
                column: "IdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationPassengers_IdClient",
                table: "ReservationPassengers",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationPassengers_IdReservation",
                table: "ReservationPassengers",
                column: "IdReservation");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationPassengers_IdSociete",
                table: "ReservationPassengers",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_DateReservation",
                table: "Reservations",
                column: "dateReservation");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IdClient",
                table: "Reservations",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IdSite",
                table: "Reservations",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IdSociete",
                table: "Reservations",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IdUtilisateur",
                table: "Reservations",
                column: "IdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IdVoyage",
                table: "Reservations",
                column: "IdVoyage");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_StatutReservation",
                table: "Reservations",
                column: "StatutReservation");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_IdPermission",
                table: "RolePermissions",
                column: "IdPermission");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_IdRole",
                table: "RolePermissions",
                column: "IdRole");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nom",
                table: "Roles",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sieges_IdCategorieSiege",
                table: "Sieges",
                column: "IdCategorieSiege");

            migrationBuilder.CreateIndex(
                name: "IX_Sieges_IdSociete",
                table: "Sieges",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Sieges_Vehicule_CodeSiege_Unique",
                table: "Sieges",
                columns: new[] { "IdVehicule", "CodeSiege" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sieges_Vehicule_NumeroOrdre_Unique",
                table: "Sieges",
                columns: new[] { "IdVehicule", "NumeroOrdre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_IdSociete",
                table: "Sites",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Societe_CodeSite_Unique",
                table: "Sites",
                columns: new[] { "IdSociete", "CodeSite" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Statut",
                table: "Sites",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Ville",
                table: "Sites",
                column: "Ville");

            migrationBuilder.CreateIndex(
                name: "IX_SmsLogs_UtilisateurIdUtilisateur",
                table: "SmsLogs",
                column: "UtilisateurIdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_TypeVehicules_IdSociete",
                table: "TypeVehicules",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_TypeVehicules_Libelle",
                table: "TypeVehicules",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_TypeVehicules_Societe_Libelle_Unique",
                table: "TypeVehicules",
                columns: new[] { "IdSociete", "Libelle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_IdUtilisateur",
                table: "UserDevices",
                column: "IdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_AttribueParIdUtilisateur",
                table: "UserPermissions",
                column: "AttribueParIdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_IdPermission",
                table: "UserPermissions",
                column: "IdPermission");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_IdUtilisateur",
                table: "UserPermissions",
                column: "IdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_IdRole",
                table: "UserRoles",
                column: "IdRole");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_IdUtilisateur",
                table: "UserRoles",
                column: "IdUtilisateur");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_Utilisateur_Role_Unique",
                table: "UserRoles",
                columns: new[] { "IdUtilisateur", "IdRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_Utilisateur_Statut",
                table: "UserRoles",
                columns: new[] { "IdUtilisateur", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Email_Unique",
                table: "Utilisateurs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_IdAgent",
                table: "Utilisateurs",
                column: "IdAgent");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_IdClient",
                table: "Utilisateurs",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_IdRole",
                table: "Utilisateurs",
                column: "IdRole");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_IdSite",
                table: "Utilisateurs",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_IdSociete",
                table: "Utilisateurs",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_IdSociete",
                table: "Vehicules",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_IdTypeVehicule",
                table: "Vehicules",
                column: "IdTypeVehicule");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_Societe_AliasVehicule_Unique",
                table: "Vehicules",
                columns: new[] { "IdSociete", "AliasVehicule" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoyageDestinations_IdDestination",
                table: "VoyageDestinations",
                column: "IdDestination");

            migrationBuilder.CreateIndex(
                name: "IX_VoyageDestinations_IdSociete",
                table: "VoyageDestinations",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_VoyageDestinations_Voyage_Ordre_Unique",
                table: "VoyageDestinations",
                columns: new[] { "IdVoyage", "Ordre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Voyages_DateDepart",
                table: "Voyages",
                column: "date_depart");

            migrationBuilder.CreateIndex(
                name: "IX_Voyages_IdDestination",
                table: "Voyages",
                column: "IdDestination");

            migrationBuilder.CreateIndex(
                name: "IX_Voyages_IdSociete",
                table: "Voyages",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_Voyages_IdVehicule",
                table: "Voyages",
                column: "IdVehicule");

            migrationBuilder.CreateIndex(
                name: "IX_VoyageSeatAllocations_IdSiege",
                table: "VoyageSeatAllocations",
                column: "IdSiege");

            migrationBuilder.CreateIndex(
                name: "IX_VoyageSeatAllocations_IdVoyage",
                table: "VoyageSeatAllocations",
                column: "IdVoyage");

            migrationBuilder.CreateIndex(
                name: "IX_VoyageSeatAllocations_ReservationPassenger_Unique",
                table: "VoyageSeatAllocations",
                column: "IdReservationPassenger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoyageSeatAllocations_Voyage_Siege_Unique",
                table: "VoyageSeatAllocations",
                columns: new[] { "IdVoyage", "IdSiege" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoyageTarifCategorieSieges_IdSociete",
                table: "VoyageTarifsCategorieSiege",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_VoyageTarifCategorieSieges_Voyage_Categorie_Unique",
                table: "VoyageTarifsCategorieSiege",
                columns: new[] { "IdVoyage", "IdCategorieSiege" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoyageTarifsCategorieSiege_IdCategorieSiege",
                table: "VoyageTarifsCategorieSiege",
                column: "IdCategorieSiege");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BilletEmbarquements");

            migrationBuilder.DropTable(
                name: "CommunicationCampaigns");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Paiements");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "PlainteClients");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SmsLogs");

            migrationBuilder.DropTable(
                name: "UserDevices");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "VoyageDestinations");

            migrationBuilder.DropTable(
                name: "VoyageSeatAllocations");

            migrationBuilder.DropTable(
                name: "VoyageTarifsCategorieSiege");

            migrationBuilder.DropTable(
                name: "Billets");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "ReservationPassengers");

            migrationBuilder.DropTable(
                name: "Sieges");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "CategorieSieges");

            migrationBuilder.DropTable(
                name: "Utilisateurs");

            migrationBuilder.DropTable(
                name: "Voyages");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Destinations");

            migrationBuilder.DropTable(
                name: "Vehicules");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "TypeVehicules");

            migrationBuilder.DropTable(
                name: "Societes");
        }
    }
}
