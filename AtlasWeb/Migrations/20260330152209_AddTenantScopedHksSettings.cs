using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantScopedHksSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HksAyarlari",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "text", nullable: false),
                    PasswordCipherText = table.Column<string>(type: "text", nullable: false),
                    ServicePasswordCipherText = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_HksAyarlari", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HksAyar_SaaS_Performance",
                table: "HksAyarlari",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksAyarlari_MusteriId_Unique",
                table: "HksAyarlari",
                column: "MusteriId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HksAyarlari");
        }
    }
}
