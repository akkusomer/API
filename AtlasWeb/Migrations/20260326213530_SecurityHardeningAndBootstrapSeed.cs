using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class SecurityHardeningAndBootstrapSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Kullanicilar"
                SET "EPosta" = LOWER(TRIM("EPosta"))
                WHERE "EPosta" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "Musteriler"
                SET "EPosta" = LOWER(TRIM("EPosta"))
                WHERE "EPosta" IS NOT NULL;
                """);

            migrationBuilder.UpdateData(
                table: "Musteriler",
                keyColumn: "Id",
                keyValue: new Guid("e06c1341-3b74-4b8c-8c6e-984bb646e297"),
                columns: new[] { "AdresDetay", "EPosta", "Ilce", "Unvan", "VergiDairesi" },
                values: new object[] { "SISTEM MERKEZI", "admin@atlasweb.local", "CANKAYA", "AtlasWeb Sistem Yonetimi", "SISTEM" });

            migrationBuilder.CreateIndex(
                name: "IX_Stoklar_MusteriId_StokKodu_Unique",
                table: "Stoklar",
                columns: new[] { "MusteriId", "StokKodu" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Musteriler_MusteriKodu_Unique",
                table: "Musteriler",
                column: "MusteriKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_EPosta_Unique",
                table: "Kullanicilar",
                column: "EPosta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CariKartlar_MusteriId_VTCK_No_Unique",
                table: "CariKartlar",
                columns: new[] { "MusteriId", "VTCK_No" },
                unique: true,
                filter: "\"VTCK_No\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stoklar_MusteriId_StokKodu_Unique",
                table: "Stoklar");

            migrationBuilder.DropIndex(
                name: "IX_Musteriler_MusteriKodu_Unique",
                table: "Musteriler");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_EPosta_Unique",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_CariKartlar_MusteriId_VTCK_No_Unique",
                table: "CariKartlar");

            migrationBuilder.UpdateData(
                table: "Musteriler",
                keyColumn: "Id",
                keyValue: new Guid("e06c1341-3b74-4b8c-8c6e-984bb646e297"),
                columns: new[] { "AdresDetay", "EPosta", "Ilce", "Unvan", "VergiDairesi" },
                values: new object[] { "SİSTEM MERKEZİ", "admin@atlasweb.com", "ÇANKAYA", "AtlasWeb Sistem Yönetimi", "SİSTEM" });
        }
    }
}
