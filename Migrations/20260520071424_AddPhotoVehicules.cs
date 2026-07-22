using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddPhotoVehicules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhotoVehicules",
                columns: table => new
                {
                    IdPhotoVehicule = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdVehicule = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypeMIME = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoVehicules", x => x.IdPhotoVehicule);
                    table.ForeignKey(
                        name: "FK_PhotoVehicules_Vehicules_IdVehicule",
                        column: x => x.IdVehicule,
                        principalTable: "Vehicules",
                        principalColumn: "IdVehicule",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoVehicules_IdVehicule",
                table: "PhotoVehicules",
                column: "IdVehicule");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoVehicules_Vehicule_Ordre_Unique",
                table: "PhotoVehicules",
                columns: new[] { "IdVehicule", "Ordre" },
                unique: true);

            // Migrer les photos existantes (Vehicules.Photo -> PhotoVehicules, Ordre = 1)
            migrationBuilder.Sql(@"
                INSERT INTO PhotoVehicules (IdVehicule, FilePath, Ordre, Statut, DateCreation, TypeMIME)
                SELECT IdVehicule, Photo, 1, 1, NOW(), 'image/jpeg'
                FROM Vehicules
                WHERE Photo IS NOT NULL AND TRIM(Photo) <> '';
            ");

            migrationBuilder.DropColumn(
                name: "Photo",
                table: "Vehicules");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Photo",
                table: "Vehicules",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
                UPDATE Vehicules v
                INNER JOIN (
                    SELECT IdVehicule, FilePath
                    FROM PhotoVehicules
                    WHERE Ordre = 1
                ) p ON v.IdVehicule = p.IdVehicule
                SET v.Photo = p.FilePath;
            ");

            migrationBuilder.DropTable(
                name: "PhotoVehicules");
        }
    }
}
