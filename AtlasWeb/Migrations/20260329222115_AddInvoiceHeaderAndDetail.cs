using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceHeaderAndDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Tutar",
                table: "Faturalar",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "FaturaNo",
                table: "Faturalar",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Aciklama",
                table: "Faturalar",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CariKartId",
                table: "Faturalar",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FaturaTarihi",
                table: "Faturalar",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Faturalar"
                SET "FaturaTarihi" = "KayitTarihi"
                WHERE "FaturaTarihi" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FaturaTarihi",
                table: "Faturalar",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "FaturaDetaylari",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FaturaId = table.Column<Guid>(type: "uuid", nullable: false),
                    StokId = table.Column<Guid>(type: "uuid", nullable: false),
                    Miktar = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SatirToplami = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_FaturaDetaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaturaDetaylari_Faturalar_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FaturaDetaylari_Stoklar_StokId",
                        column: x => x.StokId,
                        principalTable: "Stoklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_CariKartId",
                table: "Faturalar",
                column: "CariKartId");

            migrationBuilder.CreateIndex(
                name: "IX_FaturaDetay_SaaS_Performance",
                table: "FaturaDetaylari",
                columns: new[] { "MusteriId", "AktifMi", "KayitTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_FaturaDetaylari_FaturaId",
                table: "FaturaDetaylari",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_FaturaDetaylari_StokId",
                table: "FaturaDetaylari",
                column: "StokId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_CariKartlar_CariKartId",
                table: "Faturalar",
                column: "CariKartId",
                principalTable: "CariKartlar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_CariKartlar_CariKartId",
                table: "Faturalar");

            migrationBuilder.DropTable(
                name: "FaturaDetaylari");

            migrationBuilder.DropIndex(
                name: "IX_Faturalar_CariKartId",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "Aciklama",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "CariKartId",
                table: "Faturalar");

            migrationBuilder.DropColumn(
                name: "FaturaTarihi",
                table: "Faturalar");

            migrationBuilder.AlterColumn<decimal>(
                name: "Tutar",
                table: "Faturalar",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "FaturaNo",
                table: "Faturalar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }
    }
}
