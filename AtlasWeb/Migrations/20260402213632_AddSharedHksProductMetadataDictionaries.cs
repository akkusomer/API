using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedHksProductMetadataDictionaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HksUretimSekilleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HksUretimSekliId = table.Column<int>(type: "integer", nullable: false),
                    Ad = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_HksUretimSekilleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HksUrunBirimleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HksUrunBirimId = table.Column<int>(type: "integer", nullable: false),
                    Ad = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_HksUrunBirimleri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HksUrunCinsleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HksUrunCinsiId = table.Column<int>(type: "integer", nullable: false),
                    HksUrunId = table.Column<int>(type: "integer", nullable: false),
                    HksUretimSekliId = table.Column<int>(type: "integer", nullable: true),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    UrunKodu = table.Column<string>(type: "text", nullable: true),
                    IthalMi = table.Column<bool>(type: "boolean", nullable: true),
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
                    table.PrimaryKey("PK_HksUrunCinsleri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HksUretimSekilleri_AktifMi_KayitTarihi",
                table: "HksUretimSekilleri",
                columns: new[] { "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksUretimSekilleri_HksUretimSekliId_Unique",
                table: "HksUretimSekilleri",
                column: "HksUretimSekliId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HksUrunBirimleri_AktifMi_KayitTarihi",
                table: "HksUrunBirimleri",
                columns: new[] { "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksUrunBirimleri_HksUrunBirimId_Unique",
                table: "HksUrunBirimleri",
                column: "HksUrunBirimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HksUrunCinsleri_HksUrunCinsiId_Unique",
                table: "HksUrunCinsleri",
                column: "HksUrunCinsiId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HksUrunCinsleri_HksUrunId_AktifMi_KayitTarihi",
                table: "HksUrunCinsleri",
                columns: new[] { "HksUrunId", "AktifMi", "KayitTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HksUretimSekilleri");

            migrationBuilder.DropTable(
                name: "HksUrunBirimleri");

            migrationBuilder.DropTable(
                name: "HksUrunCinsleri");
        }
    }
}
