using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddCariHksLocationIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HksBeldeId",
                table: "CariKartlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HksIlId",
                table: "CariKartlar",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HksIlceId",
                table: "CariKartlar",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE public."CariKartlar" ck
                SET "HksIlId" = hi."HksIlId"
                FROM public."HksIller" hi
                WHERE ck."HksIlId" IS NULL
                  AND ck."Il" IS NOT NULL
                  AND hi."AktifMi" = TRUE
                  AND UPPER(TRIM(ck."Il")) = UPPER(TRIM(hi."Ad"));
                """);

            migrationBuilder.Sql("""
                UPDATE public."CariKartlar" ck
                SET "HksIlceId" = hic."HksIlceId"
                FROM public."HksIlceler" hic
                WHERE ck."HksIlceId" IS NULL
                  AND ck."HksIlId" IS NOT NULL
                  AND ck."Ilce" IS NOT NULL
                  AND hic."AktifMi" = TRUE
                  AND hic."HksIlId" = ck."HksIlId"
                  AND UPPER(TRIM(ck."Ilce")) = UPPER(TRIM(hic."Ad"));
                """);

            migrationBuilder.Sql("""
                UPDATE public."CariKartlar" ck
                SET "HksBeldeId" = hb."HksBeldeId"
                FROM public."HksBeldeler" hb
                WHERE ck."HksBeldeId" IS NULL
                  AND ck."HksIlceId" IS NOT NULL
                  AND ck."Belde" IS NOT NULL
                  AND hb."AktifMi" = TRUE
                  AND hb."HksIlceId" = ck."HksIlceId"
                  AND UPPER(TRIM(ck."Belde")) = UPPER(TRIM(hb."Ad"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HksBeldeId",
                table: "CariKartlar");

            migrationBuilder.DropColumn(
                name: "HksIlId",
                table: "CariKartlar");

            migrationBuilder.DropColumn(
                name: "HksIlceId",
                table: "CariKartlar");
        }
    }
}
