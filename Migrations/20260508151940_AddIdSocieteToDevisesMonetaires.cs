using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddIdSocieteToDevisesMonetaires : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdSociete",
                table: "DevisesMonetaires",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevisesMonetaires_IdSociete",
                table: "DevisesMonetaires",
                column: "IdSociete");

            migrationBuilder.AddForeignKey(
                name: "FK_DevisesMonetaires_Societes_IdSociete",
                table: "DevisesMonetaires",
                column: "IdSociete",
                principalTable: "Societes",
                principalColumn: "IdSociete",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DevisesMonetaires_Societes_IdSociete",
                table: "DevisesMonetaires");

            migrationBuilder.DropIndex(
                name: "IX_DevisesMonetaires_IdSociete",
                table: "DevisesMonetaires");

            migrationBuilder.DropColumn(
                name: "IdSociete",
                table: "DevisesMonetaires");
        }
    }
}
