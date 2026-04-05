using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedHksBusinessTypesDictionary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HksIsletmeTurleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HksIsletmeTuruId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_HksIsletmeTurleri", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HksIsletmeTurleri_AktifMi_KayitTarihi",
                table: "HksIsletmeTurleri",
                columns: new[] { "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksIsletmeTurleri_HksIsletmeTuruId_Unique",
                table: "HksIsletmeTurleri",
                column: "HksIsletmeTuruId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HksIsletmeTurleri");
        }
    }
}
