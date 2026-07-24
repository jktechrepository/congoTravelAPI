using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddUtilisateurGoogleAuthFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthProvider",
                table: "Utilisateurs",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "Utilisateurs",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSubjectId",
                table: "Utilisateurs",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_AuthProvider_ExternalSubjectId",
                table: "Utilisateurs",
                columns: new[] { "AuthProvider", "ExternalSubjectId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Utilisateurs_AuthProvider_ExternalSubjectId",
                table: "Utilisateurs");

            migrationBuilder.DropColumn(
                name: "AuthProvider",
                table: "Utilisateurs");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "Utilisateurs");

            migrationBuilder.DropColumn(
                name: "ExternalSubjectId",
                table: "Utilisateurs");
        }
    }
}
