using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddCariKartAndCariTip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Musteriler\" ALTER COLUMN \"PaketTipi\" TYPE integer USING \"PaketTipi\"::integer;");
            migrationBuilder.AlterColumn<int>(
                name: "PaketTipi",
                table: "Musteriler",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql("ALTER TABLE \"Musteriler\" ALTER COLUMN \"KimlikTuru\" TYPE integer USING \"KimlikTuru\"::integer;");
            migrationBuilder.AlterColumn<int>(
                name: "KimlikTuru",
                table: "Musteriler",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Soyad",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Ad",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.Sql("ALTER TABLE \"Faturalar\" ALTER COLUMN \"Id\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Faturalar\" ALTER COLUMN \"Id\" TYPE uuid USING (md5(\"Id\"::text)::uuid);");
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

            migrationBuilder.CreateTable(
                name: "Birimler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    Sembol = table.Column<string>(type: "text", nullable: false),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    SilinmeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SilenKullanici = table.Column<string>(type: "text", nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullanici = table.Column<string>(type: "text", nullable: true),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullanici = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Birimler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CariTipler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Adi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    SilinmeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SilenKullanici = table.Column<string>(type: "text", nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullanici = table.Column<string>(type: "text", nullable: true),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullanici = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariTipler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HataMesaji = table.Column<string>(type: "text", nullable: false),
                    HataDetayi = table.Column<string>(type: "text", nullable: true),
                    IstekYolu = table.Column<string>(type: "text", nullable: true),
                    KullaniciId = table.Column<string>(type: "text", nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stoklar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StokKodu = table.Column<string>(type: "text", nullable: false),
                    StokAdi = table.Column<string>(type: "text", nullable: false),
                    YedekAdi = table.Column<string>(type: "text", nullable: true),
                    BirimId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_Stoklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stoklar_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CariKartlar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CariTipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Unvan = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    AdiSoyadi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FaturaTipi = table.Column<int>(type: "integer", nullable: false),
                    GrupKodu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OzelKodu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefon2 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Gsm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Adres = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    VergiDairesi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VTCK_No = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
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
                    table.PrimaryKey("PK_CariKartlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CariKartlar_CariTipler_CariTipId",
                        column: x => x.CariTipId,
                        principalTable: "CariTipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_MusteriId_FaturaNo_Unique",
                table: "Faturalar",
                columns: new[] { "MusteriId", "FaturaNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CariKart_SaaS_Performance",
                table: "CariKartlar",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_CariKartlar_CariTipId",
                table: "CariKartlar",
                column: "CariTipId");

            migrationBuilder.CreateIndex(
                name: "IX_Stok_SaaS_Performance",
                table: "Stoklar",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Stoklar_BirimId",
                table: "Stoklar",
                column: "BirimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CariKartlar");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "Stoklar");

            migrationBuilder.DropTable(
                name: "CariTipler");

            migrationBuilder.DropTable(
                name: "Birimler");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_MusteriId_FaturaNo_Unique",
                table: "Faturalar");

            migrationBuilder.AlterColumn<string>(
                name: "PaketTipi",
                table: "Musteriler",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "KimlikTuru",
                table: "Musteriler",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Soyad",
                table: "Kullanicilar",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Ad",
                table: "Kullanicilar",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Faturalar",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
