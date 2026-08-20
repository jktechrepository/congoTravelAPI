using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    [DbContext(typeof(Data.CongoTravelDbContext))]
    [Migration("20260820081000_AddReversementSiteModulePaiementSource")]
    public partial class AddReversementSiteModulePaiementSource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModulePaiement",
                table: "ReversementsSite",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "IdPaiementSource",
                table: "ReversementsSite",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE ReversementsSite
SET ModulePaiement = 'Transport',
    IdPaiementSource = IdPaiement
WHERE IdPaiement IS NOT NULL
  AND (ModulePaiement IS NULL OR IdPaiementSource IS NULL);
");

            migrationBuilder.CreateIndex(
                name: "IX_ReversementSite_Module_IdPaiementSource",
                table: "ReversementsSite",
                columns: new[] { "ModulePaiement", "IdPaiementSource" },
                unique: true,
                filter: "[ModulePaiement] IS NOT NULL AND [IdPaiementSource] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReversementSite_Module_IdPaiementSource",
                table: "ReversementsSite");

            migrationBuilder.DropColumn(
                name: "ModulePaiement",
                table: "ReversementsSite");

            migrationBuilder.DropColumn(
                name: "IdPaiementSource",
                table: "ReversementsSite");
        }
    }
}
