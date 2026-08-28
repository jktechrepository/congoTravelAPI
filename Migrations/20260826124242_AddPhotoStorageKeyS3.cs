using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CongoTravel.Migrations
{
    public partial class AddPhotoStorageKeyS3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "PhotoData",
                table: "SiteTouristiqueLieuPhotos",
                type: "mediumblob",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob");

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "SiteTouristiqueLieuPhotos",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<byte[]>(
                name: "PhotoData",
                table: "RestaurantPhotos",
                type: "mediumblob",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob");

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "RestaurantPhotos",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<byte[]>(
                name: "PhotoData",
                table: "PhotoVehicules",
                type: "mediumblob",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob");

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "PhotoVehicules",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<byte[]>(
                name: "PhotoData",
                table: "EvenementSessionPhotos",
                type: "mediumblob",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob");

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "EvenementSessionPhotos",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "SiteTouristiqueLieuPhotos");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "RestaurantPhotos");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "PhotoVehicules");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "EvenementSessionPhotos");

            migrationBuilder.AlterColumn<byte[]>(
                name: "PhotoData",
                table: "SiteTouristiqueLieuPhotos",
                type: "mediumblob",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "mediumblob",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PhotoData",
                table: "RestaurantPhotos",
                type: "mediumblob",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "mediumblob",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PhotoData",
                table: "PhotoVehicules",
                type: "mediumblob",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "mediumblob",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PhotoData",
                table: "EvenementSessionPhotos",
                type: "mediumblob",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "mediumblob",
                oldNullable: true);
        }
    }
}
