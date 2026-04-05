using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddCariBirthDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DogumTarihi",
                table: "CariKartlar",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DogumTarihi",
                table: "CariKartlar");
        }
    }
}
