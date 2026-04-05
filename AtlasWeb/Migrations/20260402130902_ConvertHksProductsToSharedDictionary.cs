using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class ConvertHksProductsToSharedDictionary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HksUrun_SaaS_Performance",
                table: "HksUrunler");

            migrationBuilder.DropIndex(
                name: "IX_HksUrunler_MusteriId_HksUrunId_Unique",
                table: "HksUrunler");

            migrationBuilder.DropColumn(
                name: "MusteriId",
                table: "HksUrunler");

            migrationBuilder.Sql(
                """
                WITH deduplicated AS (
                    SELECT
                        ctid,
                        ROW_NUMBER() OVER (
                            PARTITION BY "HksUrunId"
                            ORDER BY
                                "AktifMi" DESC,
                                COALESCE("GuncellemeTarihi", "KayitTarihi") DESC,
                                "KayitTarihi" DESC,
                                "Id" DESC
                        ) AS rn
                    FROM "HksUrunler"
                )
                DELETE FROM "HksUrunler" target
                USING deduplicated source
                WHERE target.ctid = source.ctid
                  AND source.rn > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_HksUrunler_AktifMi_KayitTarihi",
                table: "HksUrunler",
                columns: new[] { "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksUrunler_HksUrunId_Unique",
                table: "HksUrunler",
                column: "HksUrunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HksUrunler_AktifMi_KayitTarihi",
                table: "HksUrunler");

            migrationBuilder.DropIndex(
                name: "IX_HksUrunler_HksUrunId_Unique",
                table: "HksUrunler");

            migrationBuilder.AddColumn<Guid>(
                name: "MusteriId",
                table: "HksUrunler",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_HksUrun_SaaS_Performance",
                table: "HksUrunler",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksUrunler_MusteriId_HksUrunId_Unique",
                table: "HksUrunler",
                columns: new[] { "MusteriId", "HksUrunId" },
                unique: true);
        }
    }
}
