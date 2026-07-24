using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class EvenementIdSiteSessionReservationPayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdSite",
                table: "EvenementSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdSite",
                table: "EvenementReservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdSite",
                table: "EvenementPayments",
                type: "int",
                nullable: true);

            // Backfill sessions → site principal actif de la société (sinon premier site actif).
            migrationBuilder.Sql(@"
UPDATE `EvenementSessions` es
INNER JOIN (
    SELECT s.`IdSociete`,
           COALESCE(
               MAX(CASE WHEN s.`IsSitePrincipal` = 1 THEN s.`IdSite` END),
               MIN(s.`IdSite`)
           ) AS `IdSite`
    FROM `Sites` s
    WHERE s.`Statut` = 1
    GROUP BY s.`IdSociete`
) pick ON pick.`IdSociete` = es.`IdSociete`
SET es.`IdSite` = pick.`IdSite`
WHERE es.`IdSite` IS NULL;
");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessions_IdSite",
                table: "EvenementSessions",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementReservations_IdSite",
                table: "EvenementReservations",
                column: "IdSite");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementPayments_IdSite",
                table: "EvenementPayments",
                column: "IdSite");

            migrationBuilder.AddForeignKey(
                name: "FK_EvenementPayments_Sites_IdSite",
                table: "EvenementPayments",
                column: "IdSite",
                principalTable: "Sites",
                principalColumn: "IdSite",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvenementReservations_Sites_IdSite",
                table: "EvenementReservations",
                column: "IdSite",
                principalTable: "Sites",
                principalColumn: "IdSite",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvenementSessions_Sites_IdSite",
                table: "EvenementSessions",
                column: "IdSite",
                principalTable: "Sites",
                principalColumn: "IdSite",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvenementPayments_Sites_IdSite",
                table: "EvenementPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_EvenementReservations_Sites_IdSite",
                table: "EvenementReservations");

            migrationBuilder.DropForeignKey(
                name: "FK_EvenementSessions_Sites_IdSite",
                table: "EvenementSessions");

            migrationBuilder.DropIndex(
                name: "IX_EvenementSessions_IdSite",
                table: "EvenementSessions");

            migrationBuilder.DropIndex(
                name: "IX_EvenementReservations_IdSite",
                table: "EvenementReservations");

            migrationBuilder.DropIndex(
                name: "IX_EvenementPayments_IdSite",
                table: "EvenementPayments");

            migrationBuilder.DropColumn(
                name: "IdSite",
                table: "EvenementSessions");

            migrationBuilder.DropColumn(
                name: "IdSite",
                table: "EvenementReservations");

            migrationBuilder.DropColumn(
                name: "IdSite",
                table: "EvenementPayments");
        }
    }
}
