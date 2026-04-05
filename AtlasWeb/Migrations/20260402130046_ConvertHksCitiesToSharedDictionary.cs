using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class ConvertHksCitiesToSharedDictionary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HksIl_SaaS_Performance",
                table: "HksIller");

            migrationBuilder.DropIndex(
                name: "IX_HksIller_MusteriId_HksIlId_Unique",
                table: "HksIller");

            migrationBuilder.DropColumn(
                name: "MusteriId",
                table: "HksIller");

            migrationBuilder.Sql(
                """
                WITH deduplicated AS (
                    SELECT
                        ctid,
                        ROW_NUMBER() OVER (
                            PARTITION BY "HksIlId"
                            ORDER BY
                                "AktifMi" DESC,
                                COALESCE("GuncellemeTarihi", "KayitTarihi") DESC,
                                "KayitTarihi" DESC,
                                "Id" DESC
                        ) AS rn
                    FROM "HksIller"
                )
                DELETE FROM "HksIller" target
                USING deduplicated source
                WHERE target.ctid = source.ctid
                  AND source.rn > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_HksIller_AktifMi_KayitTarihi",
                table: "HksIller",
                columns: new[] { "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksIller_HksIlId_Unique",
                table: "HksIller",
                column: "HksIlId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HksIller_AktifMi_KayitTarihi",
                table: "HksIller");

            migrationBuilder.DropIndex(
                name: "IX_HksIller_HksIlId_Unique",
                table: "HksIller");

            migrationBuilder.AddColumn<Guid>(
                name: "MusteriId",
                table: "HksIller",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_HksIl_SaaS_Performance",
                table: "HksIller",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksIller_MusteriId_HksIlId_Unique",
                table: "HksIller",
                columns: new[] { "MusteriId", "HksIlId" },
                unique: true);
        }
    }
}
