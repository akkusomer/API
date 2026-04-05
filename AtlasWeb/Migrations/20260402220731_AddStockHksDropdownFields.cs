using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddStockHksDropdownFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HksNitelik",
                table: "Stoklar",
                type: "character varying(16)",
                maxLength: 16,
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HksNitelik",
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
    }
}
