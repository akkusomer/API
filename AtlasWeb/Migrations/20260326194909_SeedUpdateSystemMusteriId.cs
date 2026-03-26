using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class SeedUpdateSystemMusteriId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Musteriler",
                columns: new[] { "Id", "AdresDetay", "AktifMi", "EPosta", "GsmNo", "Il", "Ilce", "KayitTarihi", "KimlikTuru", "MusteriKodu", "PaketTipi", "Unvan", "VergiDairesi", "VergiNo" },
                values: new object[] { new Guid("e06c1341-3b74-4b8c-8c6e-984bb646e297"), "SİSTEM MERKEZİ", true, "admin@atlasweb.com", "0000000000", "ANKARA", "ÇANKAYA", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, "ATLASWEB", 2, "AtlasWeb Sistem Yönetimi", "SİSTEM", "00000000000" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Musteriler",
                keyColumn: "Id",
                keyValue: new Guid("e06c1341-3b74-4b8c-8c6e-984bb646e297"));
        }
    }
}
