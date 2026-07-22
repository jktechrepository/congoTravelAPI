using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class PhotoVehiculePhotoDataMediumBlob : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoData",
                table: "PhotoVehicules",
                type: "mediumblob",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE PhotoVehicules
                SET PhotoData = FROM_BASE64(PhotoBase64)
                WHERE PhotoBase64 IS NOT NULL AND TRIM(PhotoBase64) <> '';
            ");

            migrationBuilder.AlterColumn<byte[]>(
                name: "PhotoData",
                table: "PhotoVehicules",
                type: "mediumblob",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "PhotoBase64",
                table: "PhotoVehicules");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoBase64",
                table: "PhotoVehicules",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
                UPDATE PhotoVehicules
                SET PhotoBase64 = TO_BASE64(PhotoData)
                WHERE PhotoData IS NOT NULL;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoBase64",
                table: "PhotoVehicules",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "PhotoVehicules");
        }
    }
}
