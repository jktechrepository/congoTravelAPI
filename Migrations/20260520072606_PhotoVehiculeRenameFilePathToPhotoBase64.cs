using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class PhotoVehiculeRenameFilePathToPhotoBase64 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "PhotoVehicules",
                newName: "PhotoBase64");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoBase64",
                table: "PhotoVehicules",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhotoBase64",
                table: "PhotoVehicules",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.RenameColumn(
                name: "PhotoBase64",
                table: "PhotoVehicules",
                newName: "FilePath");
        }
    }
}
