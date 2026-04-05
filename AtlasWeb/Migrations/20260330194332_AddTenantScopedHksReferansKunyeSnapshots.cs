using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantScopedHksReferansKunyeSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HksReferansKunyeKayitlari",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BitisTarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IslemKodu = table.Column<string>(type: "text", nullable: true),
                    Mesaj = table.Column<string>(type: "text", nullable: true),
                    KayitSayisi = table.Column<int>(type: "integer", nullable: false),
                    ReferansKunyelerJson = table.Column<string>(type: "text", nullable: false),
                    MusteriId = table.Column<Guid>(type: "uuid", nullable: false),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullanici = table.Column<string>(type: "text", nullable: true),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullanici = table.Column<string>(type: "text", nullable: true),
                    SilinmeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SilenKullanici = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HksReferansKunyeKayitlari", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HksReferansKunyeKayit_SaaS_Performance",
                table: "HksReferansKunyeKayitlari",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksReferansKunyeKayitlari_MusteriId_Unique",
                table: "HksReferansKunyeKayitlari",
                column: "MusteriId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HksReferansKunyeKayitlari");
        }
    }
}
