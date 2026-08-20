using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    [DbContext(typeof(Data.CongoTravelDbContext))]
    [Migration("20260820173000_AddEvenementCommandeEnAttentePlanA")]
    public partial class AddEvenementCommandeEnAttentePlanA : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvenementCommandesEnAttente",
                columns: table => new
                {
                    IdEvenementCommandeEnAttente = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdEvenementSession = table.Column<int>(type: "int", nullable: false),
                    IdSite = table.Column<int>(type: "int", nullable: true),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: true),
                    IdClient = table.Column<int>(type: "int", nullable: true),
                    MethodePaiement = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    MontantTarif = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDeviseTarif = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF"),
                    MontantFlexPay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevisePaiement = table.Column<string>(type: "char(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "CDF"),
                    TauxVersDevisePaiement = table.Column<decimal>(type: "decimal(18,8)", nullable: false, defaultValue: 1m),
                    OrderNumberFlexPay = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    ReferenceFlexPay = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    PayloadMetierJson = table.Column<string>(type: "longtext", nullable: false),
                    IdPaiementEnAttente = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementCommandesEnAttente", x => x.IdEvenementCommandeEnAttente);
                    table.ForeignKey(
                        name: "FK_EvenementCommandesEnAttente_EvenementSessions_IdEvenementSession",
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
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdEvenementCommandeEnAttenteCourante",
                table: "EvenementSessionSeats",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_DateExpiration",
                table: "EvenementCommandesEnAttente",
                column: "DateExpiration");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_OrderNumberFlexPay",
                table: "EvenementCommandesEnAttente",
                column: "OrderNumberFlexPay");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_Idempotency_UQ",
                table: "EvenementCommandesEnAttente",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_Societe_Session",
                table: "EvenementCommandesEnAttente",
                columns: new[] { "IdSociete", "IdEvenementSession" });

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_IdEvenementSession",
                table: "EvenementCommandesEnAttente",
                column: "IdEvenementSession");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementCommandesEnAttente_IdSite",
                table: "EvenementCommandesEnAttente",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementPayments_IdEvenementCommandeEnAttente",
                table: "EvenementPayments",
                column: "IdEvenementCommandeEnAttente");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionSeats_IdEvenementCommandeEnAttenteCourante",
                table: "EvenementSessionSeats",
                column: "IdEvenementCommandeEnAttenteCourante");

            migrationBuilder.AddForeignKey(
                name: "FK_EvenementPayments_EvenementCommandesEnAttente_IdEvenementCommandeEnAttente",
                table: "EvenementPayments",
                column: "IdEvenementCommandeEnAttente",
                principalTable: "EvenementCommandesEnAttente",
                principalColumn: "IdEvenementCommandeEnAttente",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvenementSessionSeats_EvenementCommandesEnAttente_IdEvenementCommandeEnAttenteCourante",
                table: "EvenementSessionSeats",
                column: "IdEvenementCommandeEnAttenteCourante",
                principalTable: "EvenementCommandesEnAttente",
                principalColumn: "IdEvenementCommandeEnAttente",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EvenementCommandesEnAttente_EvenementPayments_IdPaiementEnAttente",
                table: "EvenementCommandesEnAttente",
                column: "IdPaiementEnAttente",
                principalTable: "EvenementPayments",
                principalColumn: "IdEvenementPayment",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvenementPayments_EvenementCommandesEnAttente_IdEvenementCommandeEnAttente",
                table: "EvenementPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_EvenementSessionSeats_EvenementCommandesEnAttente_IdEvenementCommandeEnAttenteCourante",
                table: "EvenementSessionSeats");

            migrationBuilder.DropForeignKey(
                name: "FK_EvenementCommandesEnAttente_EvenementPayments_IdPaiementEnAttente",
                table: "EvenementCommandesEnAttente");

            migrationBuilder.DropTable(name: "EvenementCommandesEnAttente");

            migrationBuilder.DropIndex(
                name: "IX_EvenementPayments_IdEvenementCommandeEnAttente",
                table: "EvenementPayments");

            migrationBuilder.DropIndex(
                name: "IX_EvenementSessionSeats_IdEvenementCommandeEnAttenteCourante",
                table: "EvenementSessionSeats");

            migrationBuilder.DropColumn(
                name: "IdEvenementCommandeEnAttente",
                table: "EvenementPayments");

            migrationBuilder.DropColumn(
                name: "IdEvenementCommandeEnAttenteCourante",
                table: "EvenementSessionSeats");

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
