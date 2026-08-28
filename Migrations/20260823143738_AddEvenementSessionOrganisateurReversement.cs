using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddEvenementSessionOrganisateurReversement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "IdSiteTouristiqueReservation",
                table: "SiteTouristiquePayments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "IdSiteTouristiqueCommandeEnAttente",
                table: "SiteTouristiquePayments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<int>(
                name: "IdRestaurantReservation",
                table: "RestaurantPayments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "IdRestaurantCommandeEnAttente",
                table: "RestaurantPayments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "IdEvenementCommandeEnAttenteCourante",
                table: "EvenementSessionSeats",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<bool>(
                name: "AutoReversementOrganisateur",
                table: "EvenementSessions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroMobileMoneyOrganisateur",
                table: "EvenementSessions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "VenteEnLigneActive",
                table: "EvenementSessions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdEvenementReservation",
                table: "EvenementPayments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "IdEvenementCommandeEnAttente",
                table: "EvenementPayments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "EvenementCommandesEnAttente",
                columns: table => new
                {
                    IdEvenementCommandeEnAttente = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdEvenementSession = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: true),
                    IdClient = table.Column<int>(type: "int", nullable: true),
                    MethodePaiement = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantTarif = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDeviseTarif = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantFlexPay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevisePaiement = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TauxVersDevisePaiement = table.Column<decimal>(type: "decimal(18,8)", nullable: false, defaultValue: 1m),
                    OrderNumberFlexPay = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceFlexPay = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadMetierJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdPaiementEnAttente = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementCommandesEnAttente", x => x.IdEvenementCommandeEnAttente);
                    table.ForeignKey(
                        name: "FK_EvenementCommandesEnAttente_EvenementPayments_IdPaiementEnAt~",
                        column: x => x.IdPaiementEnAttente,
                        principalTable: "EvenementPayments",
                        principalColumn: "IdEvenementPayment",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvenementCommandesEnAttente_EvenementSessions_IdEvenementSes~",
                        column: x => x.IdEvenementSession,
                        principalTable: "EvenementSessions",
                        principalColumn: "IdEvenementSession",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvenementCommandesEnAttente_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RestaurantCommandesEnAttente",
                columns: table => new
                {
                    IdRestaurantCommandeEnAttente = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdRestaurant = table.Column<int>(type: "int", nullable: false),
                    IdRestaurantCreneau = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: true),
                    IdClient = table.Column<int>(type: "int", nullable: true),
                    MethodePaiement = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantTarif = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDeviseTarif = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantFlexPay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevisePaiement = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TauxVersDevisePaiement = table.Column<decimal>(type: "decimal(18,8)", nullable: false, defaultValue: 1m),
                    OrderNumberFlexPay = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceFlexPay = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadMetierJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdPaiementEnAttente = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantCommandesEnAttente", x => x.IdRestaurantCommandeEnAttente);
                    table.ForeignKey(
                        name: "FK_RestaurantCommandesEnAttente_RestaurantCreneaux_IdRestaurant~",
                        column: x => x.IdRestaurantCreneau,
                        principalTable: "RestaurantCreneaux",
                        principalColumn: "IdRestaurantCreneau",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestaurantCommandesEnAttente_RestaurantPayments_IdPaiementEn~",
                        column: x => x.IdPaiementEnAttente,
                        principalTable: "RestaurantPayments",
                        principalColumn: "IdRestaurantPayment",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantCommandesEnAttente_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteTouristiqueCommandesEnAttente",
                columns: table => new
                {
                    IdSiteTouristiqueCommandeEnAttente = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSiteTouristiqueJournee = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: true),
                    IdClient = table.Column<int>(type: "int", nullable: true),
                    MethodePaiement = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantTarif = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDeviseTarif = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontantFlexPay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevisePaiement = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TauxVersDevisePaiement = table.Column<decimal>(type: "decimal(18,8)", nullable: false, defaultValue: 1m),
                    OrderNumberFlexPay = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceFlexPay = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadMetierJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdPaiementEnAttente = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTouristiqueCommandesEnAttente", x => x.IdSiteTouristiqueCommandeEnAttente);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueCommandesEnAttente_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueCommandesEnAttente_SiteTouristiqueJournees_Id~",
                        column: x => x.IdSiteTouristiqueJournee,
                        principalTable: "SiteTouristiqueJournees",
                        principalColumn: "IdSiteTouristiqueJournee",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueCommandesEnAttente_SiteTouristiquePayments_Id~",
                        column: x => x.IdPaiementEnAttente,
                        principalTable: "SiteTouristiquePayments",
                        principalColumn: "IdSiteTouristiquePayment",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiquePayments_IdCommandeEnAttente",
                table: "SiteTouristiquePayments",
                column: "IdSiteTouristiqueCommandeEnAttente");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPayments_IdRestaurantCommandeEnAttente",
                table: "RestaurantPayments",
                column: "IdRestaurantCommandeEnAttente");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionSeats_IdEvenementCommandeEnAttenteCourante",
                table: "EvenementSessionSeats",
                column: "IdEvenementCommandeEnAttenteCourante");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementPayments_IdEvenementCommandeEnAttente",
                table: "EvenementPayments",
                column: "IdEvenementCommandeEnAttente");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_DateExpiration",
                table: "EvenementCommandesEnAttente",
                column: "DateExpiration");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_Idempotency_UQ",
                table: "EvenementCommandesEnAttente",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_IdEvenementSession",
                table: "EvenementCommandesEnAttente",
                column: "IdEvenementSession");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_IdPaiementEnAttente",
                table: "EvenementCommandesEnAttente",
                column: "IdPaiementEnAttente");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_IdSite",
                table: "EvenementCommandesEnAttente",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_OrderNumberFlexPay",
                table: "EvenementCommandesEnAttente",
                column: "OrderNumberFlexPay");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_Societe_Session",
                table: "EvenementCommandesEnAttente",
                columns: new[] { "IdSociete", "IdEvenementSession" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCommandesEnAttente_DateExpiration",
                table: "RestaurantCommandesEnAttente",
                column: "DateExpiration");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCommandesEnAttente_Idempotency_UQ",
                table: "RestaurantCommandesEnAttente",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCommandesEnAttente_IdPaiementEnAttente",
                table: "RestaurantCommandesEnAttente",
                column: "IdPaiementEnAttente");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCommandesEnAttente_IdRestaurantCreneau",
                table: "RestaurantCommandesEnAttente",
                column: "IdRestaurantCreneau");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCommandesEnAttente_IdSite",
                table: "RestaurantCommandesEnAttente",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCommandesEnAttente_OrderNumberFlexPay",
                table: "RestaurantCommandesEnAttente",
                column: "OrderNumberFlexPay");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCommandesEnAttente_Societe_Creneau",
                table: "RestaurantCommandesEnAttente",
                columns: new[] { "IdSociete", "IdRestaurantCreneau" });

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueCommandesEnAttente_DateExpiration",
                table: "SiteTouristiqueCommandesEnAttente",
                column: "DateExpiration");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueCommandesEnAttente_Idempotency_UQ",
                table: "SiteTouristiqueCommandesEnAttente",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueCommandesEnAttente_IdPaiementEnAttente",
                table: "SiteTouristiqueCommandesEnAttente",
                column: "IdPaiementEnAttente");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueCommandesEnAttente_IdSite",
                table: "SiteTouristiqueCommandesEnAttente",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueCommandesEnAttente_IdSiteTouristiqueJournee",
                table: "SiteTouristiqueCommandesEnAttente",
                column: "IdSiteTouristiqueJournee");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueCommandesEnAttente_OrderNumberFlexPay",
                table: "SiteTouristiqueCommandesEnAttente",
                column: "OrderNumberFlexPay");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueCommandesEnAttente_Societe_Journee",
                table: "SiteTouristiqueCommandesEnAttente",
                columns: new[] { "IdSociete", "IdSiteTouristiqueJournee" });

            migrationBuilder.AddForeignKey(
                name: "FK_EvenementPayments_EvenementCommandesEnAttente_IdEvenementCom~",
                table: "EvenementPayments",
                column: "IdEvenementCommandeEnAttente",
                principalTable: "EvenementCommandesEnAttente",
                principalColumn: "IdEvenementCommandeEnAttente",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvenementSessionSeats_EvenementCommandesEnAttente_IdEvenemen~",
                table: "EvenementSessionSeats",
                column: "IdEvenementCommandeEnAttenteCourante",
                principalTable: "EvenementCommandesEnAttente",
                principalColumn: "IdEvenementCommandeEnAttente",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantPayments_RestaurantCommandesEnAttente_IdRestaurant~",
                table: "RestaurantPayments",
                column: "IdRestaurantCommandeEnAttente",
                principalTable: "RestaurantCommandesEnAttente",
                principalColumn: "IdRestaurantCommandeEnAttente",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteTouristiquePayments_SiteTouristiqueCommandesEnAttente_Id~",
                table: "SiteTouristiquePayments",
                column: "IdSiteTouristiqueCommandeEnAttente",
                principalTable: "SiteTouristiqueCommandesEnAttente",
                principalColumn: "IdSiteTouristiqueCommandeEnAttente",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvenementPayments_EvenementCommandesEnAttente_IdEvenementCom~",
                table: "EvenementPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_EvenementSessionSeats_EvenementCommandesEnAttente_IdEvenemen~",
                table: "EvenementSessionSeats");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantPayments_RestaurantCommandesEnAttente_IdRestaurant~",
                table: "RestaurantPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteTouristiquePayments_SiteTouristiqueCommandesEnAttente_Id~",
                table: "SiteTouristiquePayments");

            migrationBuilder.DropTable(
                name: "EvenementCommandesEnAttente");

            migrationBuilder.DropTable(
                name: "RestaurantCommandesEnAttente");

            migrationBuilder.DropTable(
                name: "SiteTouristiqueCommandesEnAttente");

            migrationBuilder.DropIndex(
                name: "IX_SiteTouristiquePayments_IdCommandeEnAttente",
                table: "SiteTouristiquePayments");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantPayments_IdRestaurantCommandeEnAttente",
                table: "RestaurantPayments");

            migrationBuilder.DropIndex(
                name: "IX_EvenementSessionSeats_IdEvenementCommandeEnAttenteCourante",
                table: "EvenementSessionSeats");

            migrationBuilder.DropIndex(
                name: "IX_EvenementPayments_IdEvenementCommandeEnAttente",
                table: "EvenementPayments");

            migrationBuilder.DropColumn(
                name: "IdSiteTouristiqueCommandeEnAttente",
                table: "SiteTouristiquePayments");

            migrationBuilder.DropColumn(
                name: "IdRestaurantCommandeEnAttente",
                table: "RestaurantPayments");

            migrationBuilder.DropColumn(
                name: "IdEvenementCommandeEnAttenteCourante",
                table: "EvenementSessionSeats");

            migrationBuilder.DropColumn(
                name: "AutoReversementOrganisateur",
                table: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "NumeroMobileMoneyOrganisateur",
                table: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "VenteEnLigneActive",
                table: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "IdEvenementCommandeEnAttente",
                table: "EvenementPayments");

            migrationBuilder.AlterColumn<int>(
                name: "IdSiteTouristiqueReservation",
                table: "SiteTouristiquePayments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdRestaurantReservation",
                table: "RestaurantPayments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdEvenementReservation",
                table: "EvenementPayments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
