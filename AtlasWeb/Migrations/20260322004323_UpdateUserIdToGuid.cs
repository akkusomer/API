using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AtlasWeb.Migrations
{
    public partial class UpdateUserIdToGuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- 1. GÜVENLİ SİLME İŞLEMLERİ ---
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"AuditLogs\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"KullaniciTokens\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Kullanicilar_MusteriId\";");

            // --- 2. GÜVENLİ İSİM DEĞİŞTİRMELER ---
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'ToplamTutar') THEN
                        ALTER TABLE ""Faturalar"" RENAME COLUMN ""ToplamTutar"" TO ""Tutar"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Fatura_Tenant_Performance') THEN
                        ALTER INDEX ""IX_Fatura_Tenant_Performance"" RENAME TO ""IX_Fatura_SaaS_Performance"";
                    END IF;
                END $$;");

            // --- 3. KULLANICILAR TABLOSU GÜNCELLEMELERİ ---
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

            migrationBuilder.Sql("UPDATE \"Kullanicilar\" SET \"MusteriId\" = '00000000-0000-0000-0000-000000000000' WHERE \"MusteriId\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "MusteriId",
                table: "Kullanicilar",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // --- 4. GÜVENLİ KOLON EKLEMELER ---
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'GuncellemeTarihi') THEN
                        ALTER TABLE ""Kullanicilar"" ADD COLUMN ""GuncellemeTarihi"" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'GuncelleyenKullanici') THEN
                        ALTER TABLE ""Kullanicilar"" ADD COLUMN ""GuncelleyenKullanici"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'OlusturanKullanici') THEN
                        ALTER TABLE ""Kullanicilar"" ADD COLUMN ""OlusturanKullanici"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'SilenKullanici') THEN
                        ALTER TABLE ""Kullanicilar"" ADD COLUMN ""SilenKullanici"" text;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'SilinmeTarihi') THEN
                        ALTER TABLE ""Kullanicilar"" ADD COLUMN ""SilinmeTarihi"" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'Source') THEN
                        ALTER TABLE ""Kullanicilar"" ADD COLUMN ""Source"" integer NOT NULL DEFAULT 0;
                    END IF;
                END $$;");

            // --- 5. FATURALAR ID DÖNÜŞÜMÜ ---
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

            // --- 6. GÜVENLİ INDEX OLUŞTURMA (HATA VEREN YER) ---
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Kullanici_SaaS_Performance\" ON \"Kullanicilar\" (\"MusteriId\", \"AktifMi\", \"KayitTarihi\");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Gerekirse geri alma kodları buraya...
        }
    }
}