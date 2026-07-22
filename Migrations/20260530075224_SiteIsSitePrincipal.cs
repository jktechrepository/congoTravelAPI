using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class SiteIsSitePrincipal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSitePrincipal",
                table: "Sites",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Sites_IdSociete_IsSitePrincipal",
                table: "Sites",
                columns: new[] { "IdSociete", "IsSitePrincipal" });

            // Backfill : un site principal par société (InfoPaiement actif > plus ancien actif > premier site).
            migrationBuilder.Sql(@"
UPDATE Sites SET IsSitePrincipal = 0;

UPDATE Sites s
INNER JOIN (
    SELECT ips.IdSociete, MIN(ips.IdSite) AS IdSite
    FROM InfoPaiementsSociete ips
    INNER JOIN Sites st ON st.IdSite = ips.IdSite AND st.IdSociete = ips.IdSociete
    WHERE ips.Statut = 1
    GROUP BY ips.IdSociete
) pick ON s.IdSociete = pick.IdSociete AND s.IdSite = pick.IdSite
SET s.IsSitePrincipal = 1;

UPDATE Sites s
INNER JOIN (
    SELECT st.IdSociete, MIN(st.IdSite) AS IdSite
    FROM Sites st
    WHERE st.Statut = 1
      AND NOT EXISTS (
          SELECT 1 FROM Sites p
          WHERE p.IdSociete = st.IdSociete AND p.IsSitePrincipal = 1)
    GROUP BY st.IdSociete
) pick ON s.IdSociete = pick.IdSociete AND s.IdSite = pick.IdSite
SET s.IsSitePrincipal = 1;

UPDATE Sites s
INNER JOIN (
    SELECT st.IdSociete, MIN(st.IdSite) AS IdSite
    FROM Sites st
    WHERE NOT EXISTS (
          SELECT 1 FROM Sites p
          WHERE p.IdSociete = st.IdSociete AND p.IsSitePrincipal = 1)
    GROUP BY st.IdSociete
) pick ON s.IdSociete = pick.IdSociete AND s.IdSite = pick.IdSite
SET s.IsSitePrincipal = 1;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sites_IdSociete_IsSitePrincipal",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "IsSitePrincipal",
                table: "Sites");
        }
    }
}
