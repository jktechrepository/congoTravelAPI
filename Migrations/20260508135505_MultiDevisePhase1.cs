using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class MultiDevisePhase1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrincipale",
                table: "Societes",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "CDF")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePaiement",
                table: "Paiements",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "CDF")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrincipale",
                table: "Paiements",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "CDF")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DatePaiement",
                table: "Paiements",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "MontantAPayeDevisePrincipale",
                table: "Paiements",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantPayeDevisePrincipale",
                table: "Paiements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResteAPayeDevisePrincipale",
                table: "Paiements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxVersDevisePrincipale",
                table: "Paiements",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.CreateTable(
                name: "DevisesMonetaires",
                columns: table => new
                {
                    IdDeviseMonetaire = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CodeDevise = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Libelle = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbole = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevisesMonetaires", x => x.IdDeviseMonetaire);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TauxChanges",
                columns: table => new
                {
                    IdTauxChange = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    CodeDeviseSource = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeDeviseCible = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Taux = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    DateEffet = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TauxChanges", x => x.IdTauxChange);
                    table.ForeignKey(
                        name: "FK_TauxChanges_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "DevisesMonetaires",
                columns: new[] { "IdDeviseMonetaire", "CodeDevise", "DateCreation", "DateModification", "Libelle", "Statut", "Symbole" },
                values: new object[] { 1, "CDF", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Franc congolais", true, "FC" });

            migrationBuilder.InsertData(
                table: "DevisesMonetaires",
                columns: new[] { "IdDeviseMonetaire", "CodeDevise", "DateCreation", "DateModification", "Libelle", "Statut", "Symbole" },
                values: new object[] { 2, "USD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Dollar americain", true, "$" });

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_Societe_DevisePaiement_DatePaiement",
                table: "Paiements",
                columns: new[] { "IdSociete", "CodeDevisePaiement", "DatePaiement" });

            migrationBuilder.CreateIndex(
                name: "IX_DevisesMonetaires_CodeDevise_Unique",
                table: "DevisesMonetaires",
                column: "CodeDevise",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TauxChanges_Societe_Paire_DateEffet",
                table: "TauxChanges",
                columns: new[] { "IdSociete", "CodeDeviseSource", "CodeDeviseCible", "DateEffet" });

            migrationBuilder.Sql(@"
                UPDATE Societes
                SET CodeDevisePrincipale = 'CDF'
                WHERE CodeDevisePrincipale IS NULL OR CodeDevisePrincipale = '';
            ");

            migrationBuilder.Sql(@"
                UPDATE Paiements p
                INNER JOIN Societes s ON s.IdSociete = p.IdSociete
                SET
                    p.CodeDevisePaiement = CASE
                        WHEN p.CodeDevisePaiement IS NULL OR p.CodeDevisePaiement = '' THEN 'CDF'
                        ELSE p.CodeDevisePaiement
                    END,
                    p.CodeDevisePrincipale = CASE
                        WHEN p.CodeDevisePrincipale IS NULL OR p.CodeDevisePrincipale = '' THEN COALESCE(NULLIF(s.CodeDevisePrincipale, ''), 'CDF')
                        ELSE p.CodeDevisePrincipale
                    END,
                    p.TauxVersDevisePrincipale = CASE
                        WHEN p.TauxVersDevisePrincipale IS NULL OR p.TauxVersDevisePrincipale = 0 THEN 1
                        ELSE p.TauxVersDevisePrincipale
                    END,
                    p.MontantAPayeDevisePrincipale = CASE
                        WHEN p.MontantAPayeDevisePrincipale IS NULL OR p.MontantAPayeDevisePrincipale = 0 THEN p.MontantAPaye
                        ELSE p.MontantAPayeDevisePrincipale
                    END,
                    p.MontantPayeDevisePrincipale = CASE
                        WHEN p.MontantPaye IS NULL THEN NULL
                        WHEN p.MontantPayeDevisePrincipale IS NULL OR p.MontantPayeDevisePrincipale = 0 THEN p.MontantPaye
                        ELSE p.MontantPayeDevisePrincipale
                    END,
                    p.ResteAPayeDevisePrincipale = CASE
                        WHEN p.ResteAPaye IS NULL THEN NULL
                        WHEN p.ResteAPayeDevisePrincipale IS NULL OR p.ResteAPayeDevisePrincipale = 0 THEN p.ResteAPaye
                        ELSE p.ResteAPayeDevisePrincipale
                    END,
                    p.DatePaiement = CASE
                        WHEN p.DatePaiement = '0001-01-01 00:00:00' THEN p.DateCreation
                        ELSE p.DatePaiement
                    END;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevisesMonetaires");

            migrationBuilder.DropTable(
                name: "TauxChanges");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_Societe_DevisePaiement_DatePaiement",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrincipale",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "CodeDevisePaiement",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrincipale",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "DatePaiement",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MontantAPayeDevisePrincipale",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MontantPayeDevisePrincipale",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "ResteAPayeDevisePrincipale",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "TauxVersDevisePrincipale",
                table: "Paiements");
        }
    }
}
