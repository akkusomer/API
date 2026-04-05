using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddHksAsyncJobStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Durum",
                table: "HksReferansKunyeKayitlari",
                type: "text",
                nullable: false,
                defaultValue: "Bos");

            migrationBuilder.AddColumn<string>(
                name: "Hata",
                table: "HksReferansKunyeKayitlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgressLabel",
                table: "HksReferansKunyeKayitlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercent",
                table: "HksReferansKunyeKayitlari",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "HksReferansKunyeKayitlari"
                SET "Durum" = 'Bos'
                WHERE "Durum" IS NULL OR "Durum" = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Durum",
                table: "HksReferansKunyeKayitlari");

            migrationBuilder.DropColumn(
                name: "Hata",
                table: "HksReferansKunyeKayitlari");

            migrationBuilder.DropColumn(
                name: "ProgressLabel",
                table: "HksReferansKunyeKayitlari");

            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "HksReferansKunyeKayitlari");
        }
    }
}
