using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class EvenementSessionPhotos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvenementSessionPhotos",
                columns: table => new
                {
                    IdEvenementSessionPhoto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdEvenementSession = table.Column<int>(type: "int", nullable: false),
                    PhotoData = table.Column<byte[]>(type: "mediumblob", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypeMIME = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementSessionPhotos", x => x.IdEvenementSessionPhoto);
                    table.ForeignKey(
                        name: "FK_EvenementSessionPhotos_EvenementSessions_IdEvenementSession",
                        column: x => x.IdEvenementSession,
                        principalTable: "EvenementSessions",
                        principalColumn: "IdEvenementSession",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionPhotos_IdEvenementSession",
                table: "EvenementSessionPhotos",
                column: "IdEvenementSession");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementSessionPhotos_Session_Ordre_UQ",
                table: "EvenementSessionPhotos",
                columns: new[] { "IdEvenementSession", "Ordre" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvenementSessionPhotos");
        }
    }
}
