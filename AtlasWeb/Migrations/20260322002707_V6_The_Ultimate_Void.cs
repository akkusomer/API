using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class V6_The_Ultimate_Void : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faturalar_MusteriId",
                table: "Faturalar");

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Faturalar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "KullaniciTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KullaniciId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceInfo = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KullaniciTokens_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fatura_Tenant_Performance",
                table: "Faturalar",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciTokens_KullaniciId",
                table: "KullaniciTokens",
                column: "KullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KullaniciTokens");

            migrationBuilder.DropIndex(
                name: "IX_Fatura_Tenant_Performance",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Faturalar");

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_MusteriId",
                table: "Faturalar",
                column: "MusteriId");
        }
    }
}
