using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedHksDistrictsAndTowns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HksBeldeler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HksBeldeId = table.Column<int>(type: "integer", nullable: false),
                    HksIlceId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_HksBeldeler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HksIlceler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HksIlceId = table.Column<int>(type: "integer", nullable: false),
                    HksIlId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_HksIlceler", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HksBeldeler_HksBeldeId_Unique",
                table: "HksBeldeler",
                column: "HksBeldeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HksBeldeler_HksIlceId_AktifMi_KayitTarihi",
                table: "HksBeldeler",
                columns: new[] { "HksIlceId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksIlceler_HksIlceId_Unique",
                table: "HksIlceler",
                column: "HksIlceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HksIlceler_HksIlId_AktifMi_KayitTarihi",
                table: "HksIlceler",
                columns: new[] { "HksIlId", "AktifMi", "KayitTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HksBeldeler");

            migrationBuilder.DropTable(
                name: "HksIlceler");
        }
    }
}
