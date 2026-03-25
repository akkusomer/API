using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_Musteriler_MusteriId",
                table: "Faturalar");

            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Musteriler_MusteriId",
                table: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "KullaniciTokens");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_MusteriId",
                table: "Kullanicilar");

            migrationBuilder.RenameColumn(
                name: "ToplamTutar",
                table: "Faturalar",
                newName: "Tutar");

            migrationBuilder.RenameIndex(
                name: "IX_Fatura_Tenant_Performance",
                table: "Faturalar",
                newName: "IX_Fatura_SaaS_Performance");

            migrationBuilder.AlterColumn<string>(
                name: "Soyad",
                table: "Kullanicilar",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "MusteriId",
                table: "Kullanicilar",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Ad",
                table: "Kullanicilar",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "GuncellemeTarihi",
                table: "Kullanicilar",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuncelleyenKullanici",
                table: "Kullanicilar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OlusturanKullanici",
                table: "Kullanicilar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SilenKullanici",
                table: "Kullanicilar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SilinmeTarihi",
                table: "Kullanicilar",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Kullanicilar",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("TRUNCATE TABLE \"Faturalar\" CASCADE;");
            migrationBuilder.Sql("ALTER TABLE \"Faturalar\" ALTER COLUMN \"Id\" TYPE integer USING 0;");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Faturalar",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanici_SaaS_Performance",
                table: "Kullanicilar",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kullanici_SaaS_Performance",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "GuncellemeTarihi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "GuncelleyenKullanici",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "OlusturanKullanici",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "SilenKullanici",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "SilinmeTarihi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Kullanicilar");

            migrationBuilder.RenameColumn(
                name: "Tutar",
                table: "Faturalar",
                newName: "ToplamTutar");

            migrationBuilder.RenameIndex(
                name: "IX_Fatura_SaaS_Performance",
                table: "Faturalar",
                newName: "IX_Fatura_Tenant_Performance");

            migrationBuilder.AlterColumn<string>(
                name: "Soyad",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "MusteriId",
                table: "Kullanicilar",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "Ad",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Faturalar",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

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
                name: "IX_Kullanicilar_MusteriId",
                table: "Kullanicilar",
                column: "MusteriId");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciTokens_KullaniciId",
                table: "KullaniciTokens",
                column: "KullaniciId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Musteriler_MusteriId",
                table: "Faturalar",
                column: "MusteriId",
                principalTable: "Musteriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Musteriler_MusteriId",
                table: "Kullanicilar",
                column: "MusteriId",
                principalTable: "Musteriler",
                principalColumn: "Id");
        }
    }
}
