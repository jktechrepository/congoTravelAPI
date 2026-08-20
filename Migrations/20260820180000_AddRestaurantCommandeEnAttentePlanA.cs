using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    [DbContext(typeof(Data.CongoTravelDbContext))]
    [Migration("20260820180000_AddRestaurantCommandeEnAttentePlanA")]
    public partial class AddRestaurantCommandeEnAttentePlanA : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantCommandesEnAttente",
                columns: table => new
                {
                    IdRestaurantCommandeEnAttente = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdRestaurant = table.Column<int>(type: "int", nullable: false),
                    IdRestaurantCreneau = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RestaurantCommandesEnAttente", x => x.IdRestaurantCommandeEnAttente);
                    table.ForeignKey(name: "FK_RestaurantCommandesEnAttente_RestaurantCreneaux_IdRestaurantCreneau", column: x => x.IdRestaurantCreneau, principalTable: "RestaurantCreneaux", principalColumn: "IdRestaurantCreneau", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_RestaurantCommandesEnAttente_Sites_IdSite", column: x => x.IdSite, principalTable: "Sites", principalColumn: "IdSite", onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(name: "IdRestaurantReservation", table: "RestaurantPayments", type: "int", nullable: true, oldClrType: typeof(int), oldType: "int");
            migrationBuilder.AddColumn<Guid>(name: "IdRestaurantCommandeEnAttente", table: "RestaurantPayments", type: "char(36)", nullable: true);
            migrationBuilder.CreateIndex(name: "IX_RestaurantCommandesEnAttente_DateExpiration", table: "RestaurantCommandesEnAttente", column: "DateExpiration");
            migrationBuilder.CreateIndex(name: "IX_RestaurantCommandesEnAttente_OrderNumberFlexPay", table: "RestaurantCommandesEnAttente", column: "OrderNumberFlexPay");
            migrationBuilder.CreateIndex(name: "IX_RestaurantCommandesEnAttente_Idempotency_UQ", table: "RestaurantCommandesEnAttente", column: "IdempotencyKey", unique: true);
            migrationBuilder.CreateIndex(name: "IX_RestaurantCommandesEnAttente_Societe_Creneau", table: "RestaurantCommandesEnAttente", columns: new[] { "IdSociete", "IdRestaurantCreneau" });
            migrationBuilder.CreateIndex(name: "IX_RestaurantCommandesEnAttente_IdRestaurantCreneau", table: "RestaurantCommandesEnAttente", column: "IdRestaurantCreneau");
            migrationBuilder.CreateIndex(name: "IX_RestaurantCommandesEnAttente_IdSite", table: "RestaurantCommandesEnAttente", column: "IdSite");
            migrationBuilder.CreateIndex(name: "IX_RestaurantPayments_IdRestaurantCommandeEnAttente", table: "RestaurantPayments", column: "IdRestaurantCommandeEnAttente");
            migrationBuilder.AddForeignKey(name: "FK_RestaurantPayments_RestaurantCommandesEnAttente_IdRestaurantCommandeEnAttente", table: "RestaurantPayments", column: "IdRestaurantCommandeEnAttente", principalTable: "RestaurantCommandesEnAttente", principalColumn: "IdRestaurantCommandeEnAttente", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_RestaurantCommandesEnAttente_RestaurantPayments_IdPaiementEnAttente", table: "RestaurantCommandesEnAttente", column: "IdPaiementEnAttente", principalTable: "RestaurantPayments", principalColumn: "IdRestaurantPayment", onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_RestaurantPayments_RestaurantCommandesEnAttente_IdRestaurantCommandeEnAttente", table: "RestaurantPayments");
            migrationBuilder.DropForeignKey(name: "FK_RestaurantCommandesEnAttente_RestaurantPayments_IdPaiementEnAttente", table: "RestaurantCommandesEnAttente");
            migrationBuilder.DropTable(name: "RestaurantCommandesEnAttente");
            migrationBuilder.DropIndex(name: "IX_RestaurantPayments_IdRestaurantCommandeEnAttente", table: "RestaurantPayments");
            migrationBuilder.DropColumn(name: "IdRestaurantCommandeEnAttente", table: "RestaurantPayments");
            migrationBuilder.AlterColumn<int>(name: "IdRestaurantReservation", table: "RestaurantPayments", type: "int", nullable: false, defaultValue: 0, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        }
    }
}
