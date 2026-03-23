using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class IlkKurulum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "musteriler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MusteriKodu = table.Column<string>(type: "text", nullable: false),
                    Unvan = table.Column<string>(type: "text", nullable: false),
                    VergiNo = table.Column<string>(type: "text", nullable: false),
                    VergiDairesi = table.Column<string>(type: "text", nullable: false),
                    KimlikTuru = table.Column<string>(type: "text", nullable: false),
                    GsmNo = table.Column<string>(type: "text", nullable: false),
                    EPosta = table.Column<string>(type: "text", nullable: false),
                    Il = table.Column<string>(type: "text", nullable: false),
                    Ilce = table.Column<string>(type: "text", nullable: false),
                    AdresDetay = table.Column<string>(type: "text", nullable: false),
                    PaketTipi = table.Column<string>(type: "text", nullable: false),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_musteriler", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "musteriler");
        }
    }
}
