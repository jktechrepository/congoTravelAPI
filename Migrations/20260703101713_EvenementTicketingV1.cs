using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class EvenementTicketingV1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DureeHoldEvenementMinutes",
                table: "ConfigSocietes",
                type: "int",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.CreateTable(
                name: "EvenementClasses",
                columns: table => new
                {
                    IdEvenementClasse = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    CodeClasse = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Libelle = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementClasses", x => x.IdEvenementClasse);
                    table.ForeignKey(
                        name: "FK_EvenementClasses_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvenementSessions",
                columns: table => new
                {
                    IdEvenementSession = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    CodeSession = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Libelle = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    InventoryMode = table.Column<string>(type: "enum('SeatNumbered','ClassQuota','GlobalQuota')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "enum('Draft','Published','Closed','Cancelled')", nullable: false, defaultValue: "Draft")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementSessions", x => x.IdEvenementSession);
                    table.CheckConstraint("CK_EvenementSessions_StartEnd", "`EndAtUtc` IS NULL OR `EndAtUtc` >= `StartAtUtc`");
                    table.ForeignKey(
                        name: "FK_EvenementSessions_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvenementReservations",
                columns: table => new
                {
                    IdEvenementReservation = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdEvenementSession = table.Column<int>(type: "int", nullable: false),
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
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementReservations", x => x.IdEvenementReservation);
                    table.ForeignKey(
                        name: "FK_EvenementReservations_EvenementSessions_IdEvenementSession",
                        column: x => x.IdEvenementSession,
                        principalTable: "EvenementSessions",
                        principalColumn: "IdEvenementSession",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvenementReservations_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvenementSessionClassQuotas",
                columns: table => new
                {
                    IdEvenementSessionClassQuota = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEvenementSession = table.Column<int>(type: "int", nullable: false),
                    IdEvenementClasse = table.Column<int>(type: "int", nullable: false),
                    CapaciteTotale = table.Column<int>(type: "int", nullable: false),
                    QuantiteHold = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    QuantiteVendue = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementSessionClassQuotas", x => x.IdEvenementSessionClassQuota);
                    table.CheckConstraint("CK_EvenementSessionClassQuotas_Capacite", "`CapaciteTotale` >= 0");
                    table.CheckConstraint("CK_EvenementSessionClassQuotas_StockMax", "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
                    table.CheckConstraint("CK_EvenementSessionClassQuotas_StockPositive", "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                    table.ForeignKey(
                        name: "FK_EvenementSessionClassQuotas_EvenementClasses_IdEvenementClas~",
                        column: x => x.IdEvenementClasse,
                        principalTable: "EvenementClasses",
                        principalColumn: "IdEvenementClasse",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvenementSessionClassQuotas_EvenementSessions_IdEvenementSes~",
                        column: x => x.IdEvenementSession,
                        principalTable: "EvenementSessions",
                        principalColumn: "IdEvenementSession",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvenementSessionGlobalQuotas",
                columns: table => new
                {
                    IdEvenementSession = table.Column<int>(type: "int", nullable: false),
                    CapaciteTotale = table.Column<int>(type: "int", nullable: false),
                    QuantiteHold = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    QuantiteVendue = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementSessionGlobalQuotas", x => x.IdEvenementSession);
                    table.CheckConstraint("CK_EvenementSessionGlobalQuotas_Capacite", "`CapaciteTotale` >= 0");
                    table.CheckConstraint("CK_EvenementSessionGlobalQuotas_StockMax", "`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`");
                    table.CheckConstraint("CK_EvenementSessionGlobalQuotas_StockPositive", "`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0");
                    table.ForeignKey(
                        name: "FK_EvenementSessionGlobalQuotas_EvenementSessions_IdEvenementSe~",
                        column: x => x.IdEvenementSession,
                        principalTable: "EvenementSessions",
                        principalColumn: "IdEvenementSession",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvenementSessionSections",
                columns: table => new
                {
                    IdEvenementSessionSection = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEvenementSession = table.Column<int>(type: "int", nullable: false),
                    CodeSection = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Libelle = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementSessionSections", x => x.IdEvenementSessionSection);
                    table.ForeignKey(
                        name: "FK_EvenementSessionSections_EvenementSessions_IdEvenementSession",
                        column: x => x.IdEvenementSession,
                        principalTable: "EvenementSessions",
                        principalColumn: "IdEvenementSession",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvenementPayments",
                columns: table => new
                {
                    IdEvenementPayment = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEvenementReservation = table.Column<int>(type: "int", nullable: false),
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
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementPayments", x => x.IdEvenementPayment);
                    table.ForeignKey(
                        name: "FK_EvenementPayments_EvenementReservations_IdEvenementReservati~",
                        column: x => x.IdEvenementReservation,
                        principalTable: "EvenementReservations",
                        principalColumn: "IdEvenementReservation",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvenementSessionSeats",
                columns: table => new
                {
                    IdEvenementSessionSeat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEvenementSession = table.Column<int>(type: "int", nullable: false),
                    SeatCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdEvenementSessionSection = table.Column<int>(type: "int", nullable: true),
                    IdEvenementClasse = table.Column<int>(type: "int", nullable: true),
                    SeatStatus = table.Column<string>(type: "enum('Available','Held','Sold','Blocked')", nullable: false, defaultValue: "Available")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdEvenementReservationCourante = table.Column<int>(type: "int", nullable: true),
                    HoldExpireAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementSessionSeats", x => x.IdEvenementSessionSeat);
                    table.ForeignKey(
                        name: "FK_EvenementSessionSeats_EvenementClasses_IdEvenementClasse",
                        column: x => x.IdEvenementClasse,
                        principalTable: "EvenementClasses",
                        principalColumn: "IdEvenementClasse",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvenementSessionSeats_EvenementReservations_IdEvenementReser~",
                        column: x => x.IdEvenementReservationCourante,
                        principalTable: "EvenementReservations",
                        principalColumn: "IdEvenementReservation",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvenementSessionSeats_EvenementSessions_IdEvenementSession",
                        column: x => x.IdEvenementSession,
                        principalTable: "EvenementSessions",
                        principalColumn: "IdEvenementSession",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvenementSessionSeats_EvenementSessionSections_IdEvenementSe~",
                        column: x => x.IdEvenementSessionSection,
                        principalTable: "EvenementSessionSections",
                        principalColumn: "IdEvenementSessionSection",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvenementReservationLines",
                columns: table => new
                {
                    IdEvenementReservationLine = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEvenementReservation = table.Column<int>(type: "int", nullable: false),
                    LineType = table.Column<string>(type: "enum('Seat','ClassQuota','GlobalQuota')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevise = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdEvenementSessionSeat = table.Column<int>(type: "int", nullable: true),
                    IdEvenementSessionClassQuota = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementReservationLines", x => x.IdEvenementReservationLine);
                    table.CheckConstraint("CK_EvenementReservationLines_Quantite", "`Quantite` > 0");
                    table.ForeignKey(
                        name: "FK_EvenementReservationLines_EvenementReservations_IdEvenementR~",
                        column: x => x.IdEvenementReservation,
                        principalTable: "EvenementReservations",
                        principalColumn: "IdEvenementReservation",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvenementReservationLines_EvenementSessionClassQuotas_IdEven~",
                        column: x => x.IdEvenementSessionClassQuota,
                        principalTable: "EvenementSessionClassQuotas",
                        principalColumn: "IdEvenementSessionClassQuota",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvenementReservationLines_EvenementSessionSeats_IdEvenementS~",
                        column: x => x.IdEvenementSessionSeat,
                        principalTable: "EvenementSessionSeats",
                        principalColumn: "IdEvenementSessionSeat",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EvenementTickets",
                columns: table => new
                {
                    IdEvenementTicket = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEvenementReservationLine = table.Column<int>(type: "int", nullable: false),
                    TicketCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "enum('ISSUED','USED','VOID')", nullable: false, defaultValue: "ISSUED")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementTickets", x => x.IdEvenementTicket);
                    table.ForeignKey(
                        name: "FK_EvenementTickets_EvenementReservationLines_IdEvenementReserv~",
                        column: x => x.IdEvenementReservationLine,
                        principalTable: "EvenementReservationLines",
                        principalColumn: "IdEvenementReservationLine",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementClasses_IdSociete",
                table: "EvenementClasses",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementClasses_Societe_CodeClasse_UQ",
                table: "EvenementClasses",
                columns: new[] { "IdSociete", "CodeClasse" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementPayments_Idempotency_UQ",
                table: "EvenementPayments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementPayments_ReferencePaiement_UQ",
                table: "EvenementPayments",
                column: "ReferencePaiement",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementPayments_Reservation_Status",
                table: "EvenementPayments",
                columns: new[] { "IdEvenementReservation", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservationLines_IdEvenementReservation",
                table: "EvenementReservationLines",
                column: "IdEvenementReservation");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservationLines_IdEvenementSessionClassQuota",
                table: "EvenementReservationLines",
                column: "IdEvenementSessionClassQuota");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservationLines_IdEvenementSessionSeat",
                table: "EvenementReservationLines",
                column: "IdEvenementSessionSeat");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservationLines_Reservation_Seat_UQ",
                table: "EvenementReservationLines",
                columns: new[] { "IdEvenementReservation", "IdEvenementSessionSeat" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservations_Session_Status",
                table: "EvenementReservations",
                columns: new[] { "IdEvenementSession", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservations_Societe_Idempotency_UQ",
                table: "EvenementReservations",
                columns: new[] { "IdSociete", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservations_Societe_Reference_UQ",
                table: "EvenementReservations",
                columns: new[] { "IdSociete", "ReferenceReservation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservations_Status_ExpiresAtUtc",
                table: "EvenementReservations",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionClassQuotas_IdEvenementClasse",
                table: "EvenementSessionClassQuotas",
                column: "IdEvenementClasse");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionClassQuotas_IdEvenementSession",
                table: "EvenementSessionClassQuotas",
                column: "IdEvenementSession");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionClassQuotas_Session_Classe_UQ",
                table: "EvenementSessionClassQuotas",
                columns: new[] { "IdEvenementSession", "IdEvenementClasse" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessions_IdSociete_StartAtUtc",
                table: "EvenementSessions",
                columns: new[] { "IdSociete", "StartAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessions_Societe_CodeSession_UQ",
                table: "EvenementSessions",
                columns: new[] { "IdSociete", "CodeSession" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionSeats_HoldExpireAtUtc",
                table: "EvenementSessionSeats",
                column: "HoldExpireAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionSeats_IdEvenementClasse",
                table: "EvenementSessionSeats",
                column: "IdEvenementClasse");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionSeats_IdEvenementReservationCourante",
                table: "EvenementSessionSeats",
                column: "IdEvenementReservationCourante");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionSeats_IdEvenementSessionSection",
                table: "EvenementSessionSeats",
                column: "IdEvenementSessionSection");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionSeats_Session_SeatCode_UQ",
                table: "EvenementSessionSeats",
                columns: new[] { "IdEvenementSession", "SeatCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionSeats_Session_SeatStatus",
                table: "EvenementSessionSeats",
                columns: new[] { "IdEvenementSession", "SeatStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionSections_IdEvenementSession",
                table: "EvenementSessionSections",
                column: "IdEvenementSession");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionSections_Session_CodeSection_UQ",
                table: "EvenementSessionSections",
                columns: new[] { "IdEvenementSession", "CodeSection" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementTickets_IdEvenementReservationLine",
                table: "EvenementTickets",
                column: "IdEvenementReservationLine");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementTickets_Status",
                table: "EvenementTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementTickets_TicketCode_UQ",
                table: "EvenementTickets",
                column: "TicketCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvenementPayments");

            migrationBuilder.DropTable(
                name: "EvenementSessionGlobalQuotas");

            migrationBuilder.DropTable(
                name: "EvenementTickets");

            migrationBuilder.DropTable(
                name: "EvenementReservationLines");

            migrationBuilder.DropTable(
                name: "EvenementSessionClassQuotas");

            migrationBuilder.DropTable(
                name: "EvenementSessionSeats");

            migrationBuilder.DropTable(
                name: "EvenementClasses");

            migrationBuilder.DropTable(
                name: "EvenementReservations");

            migrationBuilder.DropTable(
                name: "EvenementSessionSections");

            migrationBuilder.DropTable(
                name: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "DureeHoldEvenementMinutes",
                table: "ConfigSocietes");
        }
    }
}
