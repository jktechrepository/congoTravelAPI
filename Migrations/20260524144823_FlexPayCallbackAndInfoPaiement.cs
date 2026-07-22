using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class FlexPayCallbackAndInfoPaiement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InfoPaiementsSociete",
                columns: table => new
                {
                    IdInfoPaiementSociete = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: false),
                    CodeMarchand = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiToken = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActifMobileMoney = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ActifCarteBancaire = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoPaiementsSociete", x => x.IdInfoPaiementSociete);
                    table.ForeignKey(
                        name: "FK_InfoPaiementsSociete_Sites_IdSite",
                        column: x => x.IdSite,
                        principalTable: "Sites",
                        principalColumn: "IdSite",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InfoPaiementsSociete_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TransactionsFlexPay",
                columns: table => new
                {
                    IdTransaction = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OrderNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderReference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypePaiement = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Channel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountCustomer = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatusFlexPay = table.Column<int>(type: "int", nullable: false),
                    CodeFlexPay = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MessageFlexPay = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatutPaiement = table.Column<int>(type: "int", nullable: false),
                    Merchant = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CallbackUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateCreationFlexPay = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateCallback = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateDerniereVerification = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: false),
                    IdCommandeReservationEnAttente = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IdPaiement = table.Column<int>(type: "int", nullable: true),
                    IdReservation = table.Column<int>(type: "int", nullable: true),
                    MessageErreur = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeHttpFlexPay = table.Column<int>(type: "int", nullable: true),
                    ReponseBruteFlexPay = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreCallbacks = table.Column<int>(type: "int", nullable: false),
                    NombreVerifications = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionsFlexPay", x => x.IdTransaction);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CallbacksFlexPay",
                columns: table => new
                {
                    IdCallback = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IdTransaction = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    OrderNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderReference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AmountCustomer = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Channel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadComplet = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Headers = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpSource = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateReception = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TraiteAvecSucces = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MessageErreur = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DetailsTraitement = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallbacksFlexPay", x => x.IdCallback);
                    table.ForeignKey(
                        name: "FK_CallbacksFlexPay_TransactionsFlexPay_IdTransaction",
                        column: x => x.IdTransaction,
                        principalTable: "TransactionsFlexPay",
                        principalColumn: "IdTransaction",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CallbackFlexPay_DateReception",
                table: "CallbacksFlexPay",
                column: "DateReception");

            migrationBuilder.CreateIndex(
                name: "IX_CallbackFlexPay_OrderNumber",
                table: "CallbacksFlexPay",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CallbacksFlexPay_IdTransaction",
                table: "CallbacksFlexPay",
                column: "IdTransaction");

            migrationBuilder.CreateIndex(
                name: "IX_InfoPaiementSociete_IdSite_Unique",
                table: "InfoPaiementsSociete",
                column: "IdSite",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfoPaiementSociete_IdSociete",
                table: "InfoPaiementsSociete",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionFlexPay_OrderNumber",
                table: "TransactionsFlexPay",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionFlexPay_Reference",
                table: "TransactionsFlexPay",
                column: "Reference");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallbacksFlexPay");

            migrationBuilder.DropTable(
                name: "InfoPaiementsSociete");

            migrationBuilder.DropTable(
                name: "TransactionsFlexPay");
        }
    }
}
