using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddBruteForceAndTokenTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Kullanicilar");

            migrationBuilder.RenameColumn(
                name: "RefreshTokenExpiryTime",
                table: "Kullanicilar",
                newName: "LockoutEnd");

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "Kullanicilar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "KullaniciTokenler",
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
                    table.PrimaryKey("PK_KullaniciTokenler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KullaniciTokenler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciTokenler_KullaniciId_Expiry",
                table: "KullaniciTokenler",
                columns: new[] { "KullaniciId", "ExpiryTime" });

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciTokenler_RefreshTokenHash_Unique",
                table: "KullaniciTokenler",
                column: "RefreshTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KullaniciTokenler");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "Kullanicilar");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                table: "Kullanicilar",
                newName: "RefreshTokenExpiryTime");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "Kullanicilar",
                type: "text",
                nullable: true);
        }
    }
}
