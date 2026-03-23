using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class V4_Architect_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KullaniciTokens");

            migrationBuilder.DropColumn(
                name: "FaturaTarihi",
                table: "Faturalar");

            migrationBuilder.RenameColumn(
                name: "TedarikciAd",
                table: "Faturalar",
                newName: "SilenKullanici");

            migrationBuilder.AddColumn<DateTime>(
                name: "GuncellemeTarihi",
                table: "Faturalar",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuncelleyenKullanici",
                table: "Faturalar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OlusturanKullanici",
                table: "Faturalar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SilinmeTarihi",
                table: "Faturalar",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "GuncellemeTarihi",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "GuncelleyenKullanici",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "OlusturanKullanici",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "SilinmeTarihi",
                table: "Faturalar");

            migrationBuilder.RenameColumn(
                name: "SilenKullanici",
                table: "Faturalar",
                newName: "TedarikciAd");

            migrationBuilder.AddColumn<DateTime>(
                name: "FaturaTarihi",
                table: "Faturalar",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "KullaniciTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KullaniciId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceInfo = table.Column<string>(type: "text", nullable: true),
                    ExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "text", nullable: false)
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
                name: "IX_KullaniciTokens_KullaniciId",
                table: "KullaniciTokens",
                column: "KullaniciId");
        }
    }
}
