using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    [DbContext(typeof(Data.CongoTravelDbContext))]
    [Migration("20260820174500_AddSiteTouristiqueCommandeEnAttentePlanA")]
    public partial class AddSiteTouristiqueCommandeEnAttentePlanA : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteTouristiqueCommandesEnAttente",
                columns: table => new
                {
                    IdSiteTouristiqueCommandeEnAttente = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdSiteTouristiqueJournee = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SiteTouristiqueCommandesEnAttente", x => x.IdSiteTouristiqueCommandeEnAttente);
                    table.ForeignKey(name: "FK_STCommandes_Journees", column: x => x.IdSiteTouristiqueJournee, principalTable: "SiteTouristiqueJournees", principalColumn: "IdSiteTouristiqueJournee", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_STCommandes_Sites", column: x => x.IdSite, principalTable: "Sites", principalColumn: "IdSite", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AlterColumn<int>(name: "IdSiteTouristiqueReservation", table: "SiteTouristiquePayments", type: "int", nullable: true, oldClrType: typeof(int), oldType: "int");
            migrationBuilder.AddColumn<Guid>(name: "IdSiteTouristiqueCommandeEnAttente", table: "SiteTouristiquePayments", type: "char(36)", nullable: true);
            migrationBuilder.CreateIndex(name: "IX_SiteTouristiqueCommandesEnAttente_DateExpiration", table: "SiteTouristiqueCommandesEnAttente", column: "DateExpiration");
            migrationBuilder.CreateIndex(name: "IX_SiteTouristiqueCommandesEnAttente_OrderNumberFlexPay", table: "SiteTouristiqueCommandesEnAttente", column: "OrderNumberFlexPay");
            migrationBuilder.CreateIndex(name: "IX_SiteTouristiqueCommandesEnAttente_Idempotency_UQ", table: "SiteTouristiqueCommandesEnAttente", column: "IdempotencyKey", unique: true);
            migrationBuilder.CreateIndex(name: "IX_SiteTouristiqueCommandesEnAttente_Societe_Journee", table: "SiteTouristiqueCommandesEnAttente", columns: new[] { "IdSociete", "IdSiteTouristiqueJournee" });
            migrationBuilder.CreateIndex(name: "IX_SiteTouristiquePayments_IdCommandeEnAttente", table: "SiteTouristiquePayments", column: "IdSiteTouristiqueCommandeEnAttente");
            migrationBuilder.AddForeignKey(name: "FK_STPayments_Commandes", table: "SiteTouristiquePayments", column: "IdSiteTouristiqueCommandeEnAttente", principalTable: "SiteTouristiqueCommandesEnAttente", principalColumn: "IdSiteTouristiqueCommandeEnAttente", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_STCommandes_Payments", table: "SiteTouristiqueCommandesEnAttente", column: "IdPaiementEnAttente", principalTable: "SiteTouristiquePayments", principalColumn: "IdSiteTouristiquePayment", onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_STPayments_Commandes", table: "SiteTouristiquePayments");
            migrationBuilder.DropForeignKey(name: "FK_STCommandes_Payments", table: "SiteTouristiqueCommandesEnAttente");
            migrationBuilder.DropTable(name: "SiteTouristiqueCommandesEnAttente");
            migrationBuilder.DropIndex(name: "IX_SiteTouristiquePayments_IdCommandeEnAttente", table: "SiteTouristiquePayments");
            migrationBuilder.DropColumn(name: "IdSiteTouristiqueCommandeEnAttente", table: "SiteTouristiquePayments");
            migrationBuilder.AlterColumn<int>(name: "IdSiteTouristiqueReservation", table: "SiteTouristiquePayments", type: "int", nullable: false, defaultValue: 0, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        }
    }
}
