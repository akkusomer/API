using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class RevertStockHksDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HksBildirimBirimi",
                table: "Stoklar");

            migrationBuilder.DropColumn(
                name: "HksEskiUrunId",
                table: "Stoklar");

            migrationBuilder.DropColumn(
                name: "HksFiyatEtkisi",
                table: "Stoklar");

            migrationBuilder.DropColumn(
                name: "HksNitelikId",
                table: "Stoklar");

            migrationBuilder.DropColumn(
                name: "HksUretimSekliId",
                table: "Stoklar");

            migrationBuilder.DropColumn(
                name: "HksUrunCinsiId",
                table: "Stoklar");

            migrationBuilder.DropColumn(
                name: "HksUrunId",
                table: "Stoklar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HksBildirimBirimi",
                table: "Stoklar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HksEskiUrunId",
                table: "Stoklar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HksFiyatEtkisi",
                table: "Stoklar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HksNitelikId",
                table: "Stoklar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HksUretimSekliId",
                table: "Stoklar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HksUrunCinsiId",
                table: "Stoklar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HksUrunId",
                table: "Stoklar",
                type: "integer",
                nullable: true);
        }
    }
}
