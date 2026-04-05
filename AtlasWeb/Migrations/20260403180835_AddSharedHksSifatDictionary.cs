using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedHksSifatDictionary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HksSifatlar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HksSifatId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_HksSifatlar", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HksSifatlar_AktifMi_KayitTarihi",
                table: "HksSifatlar",
                columns: new[] { "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_HksSifatlar_HksSifatId_Unique",
                table: "HksSifatlar",
                column: "HksSifatId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HksSifatlar");
        }
    }
}
