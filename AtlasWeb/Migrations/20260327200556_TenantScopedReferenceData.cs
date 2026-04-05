using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class TenantScopedReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stoklar_Birimler_BirimId",
                table: "Stoklar");

            migrationBuilder.AddColumn<Guid>(
                name: "MusteriId",
                table: "CariTipler",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("e06c1341-3b74-4b8c-8c6e-984bb646e297"));

            migrationBuilder.AddColumn<Guid>(
                name: "MusteriId",
                table: "Birimler",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("e06c1341-3b74-4b8c-8c6e-984bb646e297"));

            migrationBuilder.CreateIndex(
                name: "IX_CariTip_SaaS_Performance",
                table: "CariTipler",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_CariTipler_MusteriId_Adi_Unique",
                table: "CariTipler",
                columns: new[] { "MusteriId", "Adi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Birim_SaaS_Performance",
                table: "Birimler",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Birimler_MusteriId_Ad_Unique",
                table: "Birimler",
                columns: new[] { "MusteriId", "Ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Birimler_MusteriId_Sembol_Unique",
                table: "Birimler",
                columns: new[] { "MusteriId", "Sembol" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Stoklar_Birimler_BirimId",
                table: "Stoklar",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stoklar_Birimler_BirimId",
                table: "Stoklar");

            migrationBuilder.DropIndex(
                name: "IX_CariTip_SaaS_Performance",
                table: "CariTipler");

            migrationBuilder.DropIndex(
                name: "IX_CariTipler_MusteriId_Adi_Unique",
                table: "CariTipler");

            migrationBuilder.DropIndex(
                name: "IX_Birim_SaaS_Performance",
                table: "Birimler");

            migrationBuilder.DropIndex(
                name: "IX_Birimler_MusteriId_Ad_Unique",
                table: "Birimler");

            migrationBuilder.DropIndex(
                name: "IX_Birimler_MusteriId_Sembol_Unique",
                table: "Birimler");

            migrationBuilder.DropColumn(
                name: "MusteriId",
                table: "CariTipler");

            migrationBuilder.DropColumn(
                name: "MusteriId",
                table: "Birimler");

            migrationBuilder.AddForeignKey(
                name: "FK_Stoklar_Birimler_BirimId",
                table: "Stoklar",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
