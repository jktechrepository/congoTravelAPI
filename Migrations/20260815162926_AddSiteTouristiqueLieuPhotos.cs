using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddSiteTouristiqueLieuPhotos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteTouristiqueLieuPhotos",
                columns: table => new
                {
                    IdSiteTouristiqueLieuPhoto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSiteTouristique = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SiteTouristiqueLieuPhotos", x => x.IdSiteTouristiqueLieuPhoto);
                    table.ForeignKey(
                        name: "FK_SiteTouristiqueLieuPhotos_SiteTouristiques_IdSiteTouristique",
                        column: x => x.IdSiteTouristique,
                        principalTable: "SiteTouristiques",
                        principalColumn: "IdSiteTouristique",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueLieuPhotos_IdSiteTouristique",
                table: "SiteTouristiqueLieuPhotos",
                column: "IdSiteTouristique");

            migrationBuilder.CreateIndex(
                name: "IX_SiteTouristiqueLieuPhotos_Lieu_Ordre_UQ",
                table: "SiteTouristiqueLieuPhotos",
                columns: new[] { "IdSiteTouristique", "Ordre" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteTouristiqueLieuPhotos");
        }
    }
}
