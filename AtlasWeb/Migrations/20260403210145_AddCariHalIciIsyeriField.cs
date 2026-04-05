using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddCariHalIciIsyeriField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HalIciIsyeriAdi",
                table: "CariKartlar",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HksHalIciIsyeriId",
                table: "CariKartlar",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HalIciIsyeriAdi",
                table: "CariKartlar");

            migrationBuilder.DropColumn(
                name: "HksHalIciIsyeriId",
                table: "CariKartlar");
        }
    }
}
