using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddRestaurantPhotos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantPhotos",
                columns: table => new
                {
                    IdRestaurantPhoto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdRestaurant = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RestaurantPhotos", x => x.IdRestaurantPhoto);
                    table.ForeignKey(
                        name: "FK_RestaurantPhotos_Restaurants_IdRestaurant",
                        column: x => x.IdRestaurant,
                        principalTable: "Restaurants",
                        principalColumn: "IdRestaurant",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPhotos_IdRestaurant",
                table: "RestaurantPhotos",
                column: "IdRestaurant");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPhotos_Restaurant_Ordre_UQ",
                table: "RestaurantPhotos",
                columns: new[] { "IdRestaurant", "Ordre" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantPhotos");
        }
    }
}
