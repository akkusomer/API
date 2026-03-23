using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_musteriler_MusteriId",
                table: "Kullanicilar");

            migrationBuilder.DropPrimaryKey(
                name: "PK_musteriler",
                table: "musteriler");

            migrationBuilder.RenameTable(
                name: "musteriler",
                newName: "Musteriler");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "Kullanicilar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiryTime",
                table: "Kullanicilar",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Musteriler",
                table: "Musteriler",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Musteriler_MusteriId",
                table: "Kullanicilar",
                column: "MusteriId",
                principalTable: "Musteriler",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Musteriler_MusteriId",
                table: "Kullanicilar");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Musteriler",
                table: "Musteriler");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiryTime",
                table: "Kullanicilar");

            migrationBuilder.RenameTable(
                name: "Musteriler",
                newName: "musteriler");

            migrationBuilder.AddPrimaryKey(
                name: "PK_musteriler",
                table: "musteriler",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_musteriler_MusteriId",
                table: "Kullanicilar",
                column: "MusteriId",
                principalTable: "musteriler",
                principalColumn: "Id");
        }
    }
}
