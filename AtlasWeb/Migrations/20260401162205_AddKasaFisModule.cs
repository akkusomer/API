using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddKasaFisModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KasaFisleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KasaAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BelgeKodu = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BelgeNo = table.Column<int>(type: "integer", nullable: false),
                    IslemTipi = table.Column<int>(type: "integer", nullable: false),
                    CariKartId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OzelKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HareketTipi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Aciklama1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Aciklama2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Pos = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_KasaFisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KasaFisleri_CariKartlar_CariKartId",
                        column: x => x.CariKartId,
                        principalTable: "CariKartlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KasaFis_SaaS_Performance",
                table: "KasaFisleri",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_KasaFisleri_CariKartId",
                table: "KasaFisleri",
                column: "CariKartId");

            migrationBuilder.CreateIndex(
                name: "IX_KasaFisleri_MusteriId_BelgeNo_Unique",
                table: "KasaFisleri",
                columns: new[] { "MusteriId", "BelgeNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KasaFisleri");
        }
    }
}
