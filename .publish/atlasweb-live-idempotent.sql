CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321222201_IlkKurulum') THEN
    CREATE TABLE musteriler (
        "Id" uuid NOT NULL,
        "MusteriKodu" text NOT NULL,
        "Unvan" text NOT NULL,
        "VergiNo" text NOT NULL,
        "VergiDairesi" text NOT NULL,
        "KimlikTuru" text NOT NULL,
        "GsmNo" text NOT NULL,
        "EPosta" text NOT NULL,
        "Il" text NOT NULL,
        "Ilce" text NOT NULL,
        "AdresDetay" text NOT NULL,
        "PaketTipi" text NOT NULL,
        "AktifMi" boolean NOT NULL,
        "KayitTarihi" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_musteriler" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321222201_IlkKurulum') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260321222201_IlkKurulum', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321225200_KullaniciTablosuEklendi') THEN
    CREATE TABLE "Kullanicilar" (
        "Id" uuid NOT NULL,
        "Ad" text NOT NULL,
        "Soyad" text NOT NULL,
        "EPosta" text NOT NULL,
        "SifreHash" text NOT NULL,
        "Rol" text NOT NULL,
        "MusteriId" uuid,
        "AktifMi" boolean NOT NULL,
        "KayitTarihi" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Kullanicilar" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Kullanicilar_musteriler_MusteriId" FOREIGN KEY ("MusteriId") REFERENCES musteriler ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321225200_KullaniciTablosuEklendi') THEN
    CREATE INDEX "IX_Kullanicilar_MusteriId" ON "Kullanicilar" ("MusteriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321225200_KullaniciTablosuEklendi') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260321225200_KullaniciTablosuEklendi', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321233131_RefreshTokenEklendi') THEN
    ALTER TABLE "Kullanicilar" DROP CONSTRAINT "FK_Kullanicilar_musteriler_MusteriId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321233131_RefreshTokenEklendi') THEN
    ALTER TABLE musteriler DROP CONSTRAINT "PK_musteriler";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321233131_RefreshTokenEklendi') THEN
    ALTER TABLE musteriler RENAME TO "Musteriler";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321233131_RefreshTokenEklendi') THEN
    ALTER TABLE "Kullanicilar" ADD "RefreshToken" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321233131_RefreshTokenEklendi') THEN
    ALTER TABLE "Kullanicilar" ADD "RefreshTokenExpiryTime" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321233131_RefreshTokenEklendi') THEN
    ALTER TABLE "Musteriler" ADD CONSTRAINT "PK_Musteriler" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321233131_RefreshTokenEklendi') THEN
    ALTER TABLE "Kullanicilar" ADD CONSTRAINT "FK_Kullanicilar_Musteriler_MusteriId" FOREIGN KEY ("MusteriId") REFERENCES "Musteriler" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321233131_RefreshTokenEklendi') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260321233131_RefreshTokenEklendi', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321234806_V2_SaaS_Upgrade') THEN
    CREATE TABLE "Faturalar" (
        "Id" uuid NOT NULL,
        "FaturaNo" text NOT NULL,
        "TedarikciAd" text,
        "ToplamTutar" numeric NOT NULL,
        "FaturaTarihi" timestamp with time zone NOT NULL,
        "MusteriId" uuid NOT NULL,
        "AktifMi" boolean NOT NULL,
        "KayitTarihi" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Faturalar" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Faturalar_Musteriler_MusteriId" FOREIGN KEY ("MusteriId") REFERENCES "Musteriler" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321234806_V2_SaaS_Upgrade') THEN
    CREATE TABLE "KullaniciTokens" (
        "Id" uuid NOT NULL,
        "KullaniciId" uuid NOT NULL,
        "RefreshTokenHash" text NOT NULL,
        "ExpiryTime" timestamp with time zone NOT NULL,
        "DeviceInfo" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_KullaniciTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_KullaniciTokens_Kullanicilar_KullaniciId" FOREIGN KEY ("KullaniciId") REFERENCES "Kullanicilar" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321234806_V2_SaaS_Upgrade') THEN
    CREATE INDEX "IX_Faturalar_MusteriId" ON "Faturalar" ("MusteriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321234806_V2_SaaS_Upgrade') THEN
    CREATE INDEX "IX_KullaniciTokens_KullaniciId" ON "KullaniciTokens" ("KullaniciId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260321234806_V2_SaaS_Upgrade') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260321234806_V2_SaaS_Upgrade', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322001257_V4_Architect_Final') THEN
    DROP TABLE "KullaniciTokens";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322001257_V4_Architect_Final') THEN
    ALTER TABLE "Faturalar" DROP COLUMN "FaturaTarihi";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322001257_V4_Architect_Final') THEN
    ALTER TABLE "Faturalar" RENAME COLUMN "TedarikciAd" TO "SilenKullanici";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322001257_V4_Architect_Final') THEN
    ALTER TABLE "Faturalar" ADD "GuncellemeTarihi" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322001257_V4_Architect_Final') THEN
    ALTER TABLE "Faturalar" ADD "GuncelleyenKullanici" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322001257_V4_Architect_Final') THEN
    ALTER TABLE "Faturalar" ADD "OlusturanKullanici" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322001257_V4_Architect_Final') THEN
    ALTER TABLE "Faturalar" ADD "SilinmeTarihi" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322001257_V4_Architect_Final') THEN
    CREATE TABLE "AuditLogs" (
        "Id" uuid NOT NULL,
        "EntityName" text NOT NULL,
        "EntityId" text NOT NULL,
        "Action" text NOT NULL,
        "UserId" text,
        "Timestamp" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322001257_V4_Architect_Final') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260322001257_V4_Architect_Final', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322002707_V6_The_Ultimate_Void') THEN
    DROP INDEX "IX_Faturalar_MusteriId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322002707_V6_The_Ultimate_Void') THEN
    ALTER TABLE "Faturalar" ADD "Source" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322002707_V6_The_Ultimate_Void') THEN
    CREATE TABLE "KullaniciTokens" (
        "Id" uuid NOT NULL,
        "KullaniciId" uuid NOT NULL,
        "RefreshTokenHash" text NOT NULL,
        "ExpiryTime" timestamp with time zone NOT NULL,
        "DeviceInfo" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_KullaniciTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_KullaniciTokens_Kullanicilar_KullaniciId" FOREIGN KEY ("KullaniciId") REFERENCES "Kullanicilar" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322002707_V6_The_Ultimate_Void') THEN
    CREATE INDEX "IX_Fatura_Tenant_Performance" ON "Faturalar" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322002707_V6_The_Ultimate_Void') THEN
    CREATE INDEX "IX_KullaniciTokens_KullaniciId" ON "KullaniciTokens" ("KullaniciId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322002707_V6_The_Ultimate_Void') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260322002707_V6_The_Ultimate_Void', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN
    DROP TABLE IF EXISTS "AuditLogs";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN
    DROP TABLE IF EXISTS "KullaniciTokens";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN
    DROP INDEX IF EXISTS "IX_Kullanicilar_MusteriId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN

                    DO $$
                    BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'ToplamTutar') THEN
                            ALTER TABLE "Faturalar" RENAME COLUMN "ToplamTutar" TO "Tutar";
                        END IF;
                        IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Fatura_Tenant_Performance') THEN
                            ALTER INDEX "IX_Fatura_Tenant_Performance" RENAME TO "IX_Fatura_SaaS_Performance";
                        END IF;
                    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN
    ALTER TABLE "Kullanicilar" ALTER COLUMN "Soyad" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN
    ALTER TABLE "Kullanicilar" ALTER COLUMN "Ad" TYPE character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN
    UPDATE "Kullanicilar" SET "MusteriId" = '00000000-0000-0000-0000-000000000000' WHERE "MusteriId" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN
    UPDATE "Kullanicilar" SET "MusteriId" = '00000000-0000-0000-0000-000000000000' WHERE "MusteriId" IS NULL;
    ALTER TABLE "Kullanicilar" ALTER COLUMN "MusteriId" SET NOT NULL;
    ALTER TABLE "Kullanicilar" ALTER COLUMN "MusteriId" SET DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'GuncellemeTarihi') THEN
                            ALTER TABLE "Kullanicilar" ADD COLUMN "GuncellemeTarihi" timestamp with time zone;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'GuncelleyenKullanici') THEN
                            ALTER TABLE "Kullanicilar" ADD COLUMN "GuncelleyenKullanici" text;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'OlusturanKullanici') THEN
                            ALTER TABLE "Kullanicilar" ADD COLUMN "OlusturanKullanici" text;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'SilenKullanici') THEN
                            ALTER TABLE "Kullanicilar" ADD COLUMN "SilenKullanici" text;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'SilinmeTarihi') THEN
                            ALTER TABLE "Kullanicilar" ADD COLUMN "SilinmeTarihi" timestamp with time zone;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'Source') THEN
                            ALTER TABLE "Kullanicilar" ADD COLUMN "Source" integer NOT NULL DEFAULT 0;
                        END IF;
                    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'GuncellemeTarihi') THEN
                            ALTER TABLE "Faturalar" ADD COLUMN "GuncellemeTarihi" timestamp with time zone;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'GuncelleyenKullanici') THEN
                            ALTER TABLE "Faturalar" ADD COLUMN "GuncelleyenKullanici" text;
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'OlusturanKullanici') THEN
                            ALTER TABLE "Faturalar" ADD COLUMN "OlusturanKullanici" text;
                        END IF;
                    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN
    CREATE INDEX IF NOT EXISTS "IX_Kullanici_SaaS_Performance" ON "Kullanicilar" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322004323_UpdateUserIdToGuid') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260322004323_UpdateUserIdToGuid', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN

                    DO $$ BEGIN 
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Musteriler' AND column_name = 'PaketTipi' AND data_type = 'text') THEN
                            ALTER TABLE "Musteriler" ALTER COLUMN "PaketTipi" TYPE integer USING "PaketTipi"::integer;
                        END IF;
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Musteriler' AND column_name = 'KimlikTuru' AND data_type = 'text') THEN
                            ALTER TABLE "Musteriler" ALTER COLUMN "KimlikTuru" TYPE integer USING "KimlikTuru"::integer;
                        END IF;
                    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN

                    DO $$ BEGIN 
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'Soyad' AND character_maximum_length IS NOT NULL) THEN
                            ALTER TABLE "Kullanicilar" ALTER COLUMN "Soyad" TYPE text;
                        END IF;
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'Ad' AND character_maximum_length IS NOT NULL) THEN
                            ALTER TABLE "Kullanicilar" ALTER COLUMN "Ad" TYPE text;
                        END IF;
                    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN

                    DO $$ BEGIN 
                        -- Identity'i kaldır (Sadece varsa)
                        IF EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Faturalar' AND column_name = 'Id' AND is_identity = 'YES') THEN
                            ALTER TABLE "Faturalar" ALTER COLUMN "Id" DROP IDENTITY;
                        END IF;
                        
                        -- Tipi UUID'ye çevir (Sadece hala integer ise)
                        IF (SELECT data_type FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'Id') = 'integer' THEN
                            ALTER TABLE "Faturalar" ALTER COLUMN "Id" TYPE uuid USING (md5("Id"::text)::uuid);
                        END IF;
                    END $$;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE TABLE IF NOT EXISTS "AuditLogs" ( "Id" uuid NOT NULL, "EntityName" text NOT NULL, "EntityId" text NOT NULL, "Action" text NOT NULL, "UserId" text, "Timestamp" timestamp with time zone NOT NULL, CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id") );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE TABLE IF NOT EXISTS "Birimler" ( "Id" uuid NOT NULL, "Ad" text NOT NULL, "Sembol" text NOT NULL, "AktifMi" boolean NOT NULL, "SilinmeTarihi" timestamp with time zone, "SilenKullanici" text, "KayitTarihi" timestamp with time zone NOT NULL, "OlusturanKullanici" text, "GuncellemeTarihi" timestamp with time zone, "GuncelleyenKullanici" text, "Source" integer NOT NULL, CONSTRAINT "PK_Birimler" PRIMARY KEY ("Id") );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE TABLE IF NOT EXISTS "CariTipler" ( "Id" uuid NOT NULL, "Adi" character varying(50) NOT NULL, "Aciklama" character varying(200), "AktifMi" boolean NOT NULL, "SilinmeTarihi" timestamp with time zone, "SilenKullanici" text, "KayitTarihi" timestamp with time zone NOT NULL, "OlusturanKullanici" text, "GuncellemeTarihi" timestamp with time zone, "GuncelleyenKullanici" text, "Source" integer NOT NULL, CONSTRAINT "PK_CariTipler" PRIMARY KEY ("Id") );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE TABLE IF NOT EXISTS "ErrorLogs" ( "Id" integer GENERATED BY DEFAULT AS IDENTITY, "HataMesaji" text NOT NULL, "HataDetayi" text, "IstekYolu" text, "KullaniciId" text, "Tarih" timestamp with time zone NOT NULL, CONSTRAINT "PK_ErrorLogs" PRIMARY KEY ("Id") );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE TABLE IF NOT EXISTS "Stoklar" ( "Id" uuid NOT NULL, "StokKodu" text NOT NULL, "StokAdi" text NOT NULL, "YedekAdi" text, "BirimId" uuid NOT NULL, "MusteriId" uuid NOT NULL, "AktifMi" boolean NOT NULL, "KayitTarihi" timestamp with time zone NOT NULL, "OlusturanKullanici" text, "GuncellemeTarihi" timestamp with time zone, "GuncelleyenKullanici" text, "SilinmeTarihi" timestamp with time zone, "SilenKullanici" text, "Source" integer NOT NULL, CONSTRAINT "PK_Stoklar" PRIMARY KEY ("Id"), CONSTRAINT "FK_Stoklar_Birimler_BirimId" FOREIGN KEY ("BirimId") REFERENCES "Birimler" ("Id") ON DELETE CASCADE );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE TABLE IF NOT EXISTS "CariKartlar" ( "Id" uuid NOT NULL, "CariTipId" uuid NOT NULL, "Unvan" character varying(150), "AdiSoyadi" character varying(100), "FaturaTipi" integer NOT NULL, "GrupKodu" character varying(20), "OzelKodu" character varying(20), "Telefon" character varying(20), "Telefon2" character varying(20), "Gsm" character varying(20), "Adres" character varying(250), "VergiDairesi" character varying(50), "VTCK_No" character varying(11), "MusteriId" uuid NOT NULL, "AktifMi" boolean NOT NULL, "KayitTarihi" timestamp with time zone NOT NULL, "OlusturanKullanici" text, "GuncellemeTarihi" timestamp with time zone, "GuncelleyenKullanici" text, "SilinmeTarihi" timestamp with time zone, "SilenKullanici" text, "Source" integer NOT NULL, CONSTRAINT "PK_CariKartlar" PRIMARY KEY ("Id"), CONSTRAINT "FK_CariKartlar_CariTipler_CariTipId" FOREIGN KEY ("CariTipId") REFERENCES "CariTipler" ("Id") ON DELETE RESTRICT );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Faturalar_MusteriId_FaturaNo_Unique" ON "Faturalar" ("MusteriId", "FaturaNo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE INDEX IF NOT EXISTS "IX_CariKart_SaaS_Performance" ON "CariKartlar" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE INDEX IF NOT EXISTS "IX_CariKartlar_CariTipId" ON "CariKartlar" ("CariTipId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE INDEX IF NOT EXISTS "IX_Stok_SaaS_Performance" ON "Stoklar" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    CREATE INDEX IF NOT EXISTS "IX_Stoklar_BirimId" ON "Stoklar" ("BirimId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324224424_AddCariKartAndCariTip') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260324224424_AddCariKartAndCariTip', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325140318_AddBruteForceAndTokenTable') THEN
    ALTER TABLE "Kullanicilar" DROP COLUMN "RefreshToken";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325140318_AddBruteForceAndTokenTable') THEN
    ALTER TABLE "Kullanicilar" RENAME COLUMN "RefreshTokenExpiryTime" TO "LockoutEnd";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325140318_AddBruteForceAndTokenTable') THEN
    ALTER TABLE "Kullanicilar" ADD "FailedLoginCount" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325140318_AddBruteForceAndTokenTable') THEN
    CREATE TABLE "KullaniciTokenler" (
        "Id" uuid NOT NULL,
        "KullaniciId" uuid NOT NULL,
        "RefreshTokenHash" text NOT NULL,
        "ExpiryTime" timestamp with time zone NOT NULL,
        "DeviceInfo" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_KullaniciTokenler" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_KullaniciTokenler_Kullanicilar_KullaniciId" FOREIGN KEY ("KullaniciId") REFERENCES "Kullanicilar" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325140318_AddBruteForceAndTokenTable') THEN
    CREATE INDEX "IX_KullaniciTokenler_KullaniciId_Expiry" ON "KullaniciTokenler" ("KullaniciId", "ExpiryTime");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325140318_AddBruteForceAndTokenTable') THEN
    CREATE UNIQUE INDEX "IX_KullaniciTokenler_RefreshTokenHash_Unique" ON "KullaniciTokenler" ("RefreshTokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260325140318_AddBruteForceAndTokenTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260325140318_AddBruteForceAndTokenTable', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326194909_SeedUpdateSystemMusteriId') THEN
    INSERT INTO "Musteriler" ("Id", "AdresDetay", "AktifMi", "EPosta", "GsmNo", "Il", "Ilce", "KayitTarihi", "KimlikTuru", "MusteriKodu", "PaketTipi", "Unvan", "VergiDairesi", "VergiNo")
    VALUES ('e06c1341-3b74-4b8c-8c6e-984bb646e297', 'SİSTEM MERKEZİ', TRUE, 'admin@atlasweb.com', '0000000000', 'ANKARA', 'ÇANKAYA', TIMESTAMPTZ '2024-01-01T00:00:00Z', 0, 'ATLASWEB', 2, 'AtlasWeb Sistem Yönetimi', 'SİSTEM', '00000000000');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326194909_SeedUpdateSystemMusteriId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326194909_SeedUpdateSystemMusteriId', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326213530_SecurityHardeningAndBootstrapSeed') THEN
    UPDATE "Kullanicilar"
    SET "EPosta" = LOWER(TRIM("EPosta"))
    WHERE "EPosta" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326213530_SecurityHardeningAndBootstrapSeed') THEN
    UPDATE "Musteriler"
    SET "EPosta" = LOWER(TRIM("EPosta"))
    WHERE "EPosta" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326213530_SecurityHardeningAndBootstrapSeed') THEN
    UPDATE "Musteriler" SET "AdresDetay" = 'SISTEM MERKEZI', "EPosta" = 'admin@atlasweb.local', "Ilce" = 'CANKAYA', "Unvan" = 'AtlasWeb Sistem Yonetimi', "VergiDairesi" = 'SISTEM'
    WHERE "Id" = 'e06c1341-3b74-4b8c-8c6e-984bb646e297';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326213530_SecurityHardeningAndBootstrapSeed') THEN
    CREATE UNIQUE INDEX "IX_Stoklar_MusteriId_StokKodu_Unique" ON "Stoklar" ("MusteriId", "StokKodu");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326213530_SecurityHardeningAndBootstrapSeed') THEN
    CREATE UNIQUE INDEX "IX_Musteriler_MusteriKodu_Unique" ON "Musteriler" ("MusteriKodu");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326213530_SecurityHardeningAndBootstrapSeed') THEN
    CREATE UNIQUE INDEX "IX_Kullanicilar_EPosta_Unique" ON "Kullanicilar" ("EPosta");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326213530_SecurityHardeningAndBootstrapSeed') THEN
    CREATE UNIQUE INDEX "IX_CariKartlar_MusteriId_VTCK_No_Unique" ON "CariKartlar" ("MusteriId", "VTCK_No") WHERE "VTCK_No" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326213530_SecurityHardeningAndBootstrapSeed') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326213530_SecurityHardeningAndBootstrapSeed', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327200556_TenantScopedReferenceData') THEN
    ALTER TABLE "Stoklar" DROP CONSTRAINT "FK_Stoklar_Birimler_BirimId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327200556_TenantScopedReferenceData') THEN
    ALTER TABLE "CariTipler" ADD "MusteriId" uuid NOT NULL DEFAULT 'e06c1341-3b74-4b8c-8c6e-984bb646e297';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327200556_TenantScopedReferenceData') THEN
    ALTER TABLE "Birimler" ADD "MusteriId" uuid NOT NULL DEFAULT 'e06c1341-3b74-4b8c-8c6e-984bb646e297';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327200556_TenantScopedReferenceData') THEN
    CREATE INDEX "IX_CariTip_SaaS_Performance" ON "CariTipler" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327200556_TenantScopedReferenceData') THEN
    CREATE UNIQUE INDEX "IX_CariTipler_MusteriId_Adi_Unique" ON "CariTipler" ("MusteriId", "Adi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327200556_TenantScopedReferenceData') THEN
    CREATE INDEX "IX_Birim_SaaS_Performance" ON "Birimler" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327200556_TenantScopedReferenceData') THEN
    CREATE UNIQUE INDEX "IX_Birimler_MusteriId_Ad_Unique" ON "Birimler" ("MusteriId", "Ad");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327200556_TenantScopedReferenceData') THEN
    CREATE UNIQUE INDEX "IX_Birimler_MusteriId_Sembol_Unique" ON "Birimler" ("MusteriId", "Sembol");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327200556_TenantScopedReferenceData') THEN
    ALTER TABLE "Stoklar" ADD CONSTRAINT "FK_Stoklar_Birimler_BirimId" FOREIGN KEY ("BirimId") REFERENCES "Birimler" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327200556_TenantScopedReferenceData') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260327200556_TenantScopedReferenceData', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328122218_AddPasswordResetTokens') THEN
    CREATE TABLE "KullaniciSifreSifirlamaTokenler" (
        "Id" uuid NOT NULL,
        "KullaniciId" uuid NOT NULL,
        "TokenHash" text NOT NULL,
        "ExpiryTime" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ConsumedAt" timestamp with time zone,
        "RequestedIpAddress" text,
        CONSTRAINT "PK_KullaniciSifreSifirlamaTokenler" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_KullaniciSifreSifirlamaTokenler_Kullanicilar_KullaniciId" FOREIGN KEY ("KullaniciId") REFERENCES "Kullanicilar" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328122218_AddPasswordResetTokens') THEN
    CREATE INDEX "IX_KullaniciSifreSifirlamaTokenler_KullaniciId_Expiry" ON "KullaniciSifreSifirlamaTokenler" ("KullaniciId", "ExpiryTime");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328122218_AddPasswordResetTokens') THEN
    CREATE UNIQUE INDEX "IX_KullaniciSifreSifirlamaTokenler_TokenHash_Unique" ON "KullaniciSifreSifirlamaTokenler" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328122218_AddPasswordResetTokens') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260328122218_AddPasswordResetTokens', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328131126_AddUserPhoneAndAdminUpdate') THEN
    ALTER TABLE "Kullanicilar" ADD "Telefon" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260328131126_AddUserPhoneAndAdminUpdate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260328131126_AddUserPhoneAndAdminUpdate', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    ALTER TABLE "Faturalar" ALTER COLUMN "Tutar" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    ALTER TABLE "Faturalar" ALTER COLUMN "FaturaNo" TYPE character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    ALTER TABLE "Faturalar" ADD "Aciklama" character varying(300);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    ALTER TABLE "Faturalar" ADD "CariKartId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    ALTER TABLE "Faturalar" ADD "FaturaTarihi" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    UPDATE "Faturalar"
    SET "FaturaTarihi" = "KayitTarihi"
    WHERE "FaturaTarihi" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    ALTER TABLE "Faturalar" ALTER COLUMN "FaturaTarihi" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    CREATE TABLE "FaturaDetaylari" (
        "Id" uuid NOT NULL,
        "FaturaId" uuid NOT NULL,
        "StokId" uuid NOT NULL,
        "Miktar" numeric(18,3) NOT NULL,
        "BirimFiyat" numeric(18,2) NOT NULL,
        "SatirToplami" numeric(18,2) NOT NULL,
        "MusteriId" uuid NOT NULL,
        "AktifMi" boolean NOT NULL,
        "KayitTarihi" timestamp with time zone NOT NULL,
        "OlusturanKullanici" text,
        "GuncellemeTarihi" timestamp with time zone,
        "GuncelleyenKullanici" text,
        "SilinmeTarihi" timestamp with time zone,
        "SilenKullanici" text,
        "Source" integer NOT NULL,
        CONSTRAINT "PK_FaturaDetaylari" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FaturaDetaylari_Faturalar_FaturaId" FOREIGN KEY ("FaturaId") REFERENCES "Faturalar" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_FaturaDetaylari_Stoklar_StokId" FOREIGN KEY ("StokId") REFERENCES "Stoklar" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    CREATE INDEX "IX_Faturalar_CariKartId" ON "Faturalar" ("CariKartId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    CREATE INDEX "IX_FaturaDetay_SaaS_Performance" ON "FaturaDetaylari" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    CREATE INDEX "IX_FaturaDetaylari_FaturaId" ON "FaturaDetaylari" ("FaturaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    CREATE INDEX "IX_FaturaDetaylari_StokId" ON "FaturaDetaylari" ("StokId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    ALTER TABLE "Faturalar" ADD CONSTRAINT "FK_Faturalar_CariKartlar_CariKartId" FOREIGN KEY ("CariKartId") REFERENCES "CariKartlar" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329222115_AddInvoiceHeaderAndDetail') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260329222115_AddInvoiceHeaderAndDetail', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330152209_AddTenantScopedHksSettings') THEN
    CREATE TABLE "HksAyarlari" (
        "Id" uuid NOT NULL,
        "KullaniciAdi" text NOT NULL,
        "PasswordCipherText" text NOT NULL,
        "ServicePasswordCipherText" text NOT NULL,
        "MusteriId" uuid NOT NULL,
        "AktifMi" boolean NOT NULL,
        "KayitTarihi" timestamp with time zone NOT NULL,
        "OlusturanKullanici" text,
        "GuncellemeTarihi" timestamp with time zone,
        "GuncelleyenKullanici" text,
        "SilinmeTarihi" timestamp with time zone,
        "SilenKullanici" text,
        "Source" integer NOT NULL,
        CONSTRAINT "PK_HksAyarlari" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330152209_AddTenantScopedHksSettings') THEN
    CREATE INDEX "IX_HksAyar_SaaS_Performance" ON "HksAyarlari" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330152209_AddTenantScopedHksSettings') THEN
    CREATE UNIQUE INDEX "IX_HksAyarlari_MusteriId_Unique" ON "HksAyarlari" ("MusteriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330152209_AddTenantScopedHksSettings') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260330152209_AddTenantScopedHksSettings', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330194332_AddTenantScopedHksReferansKunyeSnapshots') THEN
    CREATE TABLE "HksReferansKunyeKayitlari" (
        "Id" uuid NOT NULL,
        "BaslangicTarihi" timestamp without time zone,
        "BitisTarihi" timestamp without time zone,
        "IslemKodu" text,
        "Mesaj" text,
        "KayitSayisi" integer NOT NULL,
        "ReferansKunyelerJson" text NOT NULL,
        "MusteriId" uuid NOT NULL,
        "AktifMi" boolean NOT NULL,
        "KayitTarihi" timestamp with time zone NOT NULL,
        "OlusturanKullanici" text,
        "GuncellemeTarihi" timestamp with time zone,
        "GuncelleyenKullanici" text,
        "SilinmeTarihi" timestamp with time zone,
        "SilenKullanici" text,
        "Source" integer NOT NULL,
        CONSTRAINT "PK_HksReferansKunyeKayitlari" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330194332_AddTenantScopedHksReferansKunyeSnapshots') THEN
    CREATE INDEX "IX_HksReferansKunyeKayit_SaaS_Performance" ON "HksReferansKunyeKayitlari" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330194332_AddTenantScopedHksReferansKunyeSnapshots') THEN
    CREATE UNIQUE INDEX "IX_HksReferansKunyeKayitlari_MusteriId_Unique" ON "HksReferansKunyeKayitlari" ("MusteriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330194332_AddTenantScopedHksReferansKunyeSnapshots') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260330194332_AddTenantScopedHksReferansKunyeSnapshots', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330201443_AddHksAsyncJobStatus') THEN
    ALTER TABLE "HksReferansKunyeKayitlari" ADD "Durum" text NOT NULL DEFAULT 'Bos';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330201443_AddHksAsyncJobStatus') THEN
    ALTER TABLE "HksReferansKunyeKayitlari" ADD "Hata" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330201443_AddHksAsyncJobStatus') THEN
    ALTER TABLE "HksReferansKunyeKayitlari" ADD "ProgressLabel" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330201443_AddHksAsyncJobStatus') THEN
    ALTER TABLE "HksReferansKunyeKayitlari" ADD "ProgressPercent" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330201443_AddHksAsyncJobStatus') THEN
    UPDATE "HksReferansKunyeKayitlari"
    SET "Durum" = 'Bos'
    WHERE "Durum" IS NULL OR "Durum" = '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330201443_AddHksAsyncJobStatus') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260330201443_AddHksAsyncJobStatus', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330203818_AddStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" ADD "HksBildirimBirimi" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330203818_AddStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" ADD "HksEskiUrunId" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330203818_AddStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" ADD "HksFiyatEtkisi" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330203818_AddStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" ADD "HksNitelikId" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330203818_AddStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" ADD "HksUretimSekliId" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330203818_AddStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" ADD "HksUrunCinsiId" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330203818_AddStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" ADD "HksUrunId" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330203818_AddStockHksDefinitions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260330203818_AddStockHksDefinitions', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330205642_RevertStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" DROP COLUMN "HksBildirimBirimi";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330205642_RevertStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" DROP COLUMN "HksEskiUrunId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330205642_RevertStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" DROP COLUMN "HksFiyatEtkisi";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330205642_RevertStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" DROP COLUMN "HksNitelikId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330205642_RevertStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" DROP COLUMN "HksUretimSekliId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330205642_RevertStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" DROP COLUMN "HksUrunCinsiId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330205642_RevertStockHksDefinitions') THEN
    ALTER TABLE "Stoklar" DROP COLUMN "HksUrunId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330205642_RevertStockHksDefinitions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260330205642_RevertStockHksDefinitions', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330211708_AddTenantScopedHksProducts') THEN
    CREATE TABLE "HksUrunler" (
        "Id" uuid NOT NULL,
        "HksUrunId" integer NOT NULL,
        "Ad" text NOT NULL,
        "MusteriId" uuid NOT NULL,
        "AktifMi" boolean NOT NULL,
        "KayitTarihi" timestamp with time zone NOT NULL,
        "OlusturanKullanici" text,
        "GuncellemeTarihi" timestamp with time zone,
        "GuncelleyenKullanici" text,
        "SilinmeTarihi" timestamp with time zone,
        "SilenKullanici" text,
        "Source" integer NOT NULL,
        CONSTRAINT "PK_HksUrunler" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330211708_AddTenantScopedHksProducts') THEN
    CREATE INDEX "IX_HksUrun_SaaS_Performance" ON "HksUrunler" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330211708_AddTenantScopedHksProducts') THEN
    CREATE UNIQUE INDEX "IX_HksUrunler_MusteriId_HksUrunId_Unique" ON "HksUrunler" ("MusteriId", "HksUrunId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260330211708_AddTenantScopedHksProducts') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260330211708_AddTenantScopedHksProducts', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401150227_AddInvoiceTahsilatAmount') THEN
    ALTER TABLE "Faturalar" ADD "TahsilEdilenTutar" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401150227_AddInvoiceTahsilatAmount') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260401150227_AddInvoiceTahsilatAmount', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401162205_AddKasaFisModule') THEN
    CREATE TABLE "KasaFisleri" (
        "Id" uuid NOT NULL,
        "KasaAdi" character varying(100) NOT NULL,
        "BelgeKodu" character varying(10) NOT NULL,
        "BelgeNo" integer NOT NULL,
        "IslemTipi" integer NOT NULL,
        "CariKartId" uuid,
        "Tarih" timestamp with time zone NOT NULL,
        "OzelKodu" character varying(50),
        "HareketTipi" character varying(50) NOT NULL,
        "Aciklama1" character varying(200),
        "Aciklama2" character varying(200),
        "Pos" character varying(50),
        "Tutar" numeric(18,2) NOT NULL,
        "MusteriId" uuid NOT NULL,
        "AktifMi" boolean NOT NULL,
        "KayitTarihi" timestamp with time zone NOT NULL,
        "OlusturanKullanici" text,
        "GuncellemeTarihi" timestamp with time zone,
        "GuncelleyenKullanici" text,
        "SilinmeTarihi" timestamp with time zone,
        "SilenKullanici" text,
        "Source" integer NOT NULL,
        CONSTRAINT "PK_KasaFisleri" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_KasaFisleri_CariKartlar_CariKartId" FOREIGN KEY ("CariKartId") REFERENCES "CariKartlar" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401162205_AddKasaFisModule') THEN
    CREATE INDEX "IX_KasaFis_SaaS_Performance" ON "KasaFisleri" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401162205_AddKasaFisModule') THEN
    CREATE INDEX "IX_KasaFisleri_CariKartId" ON "KasaFisleri" ("CariKartId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401162205_AddKasaFisModule') THEN
    CREATE UNIQUE INDEX "IX_KasaFisleri_MusteriId_BelgeNo_Unique" ON "KasaFisleri" ("MusteriId", "BelgeNo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260401162205_AddKasaFisModule') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260401162205_AddKasaFisModule', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402121512_AddTenantScopedHksCities') THEN
    CREATE TABLE "HksIller" (
        "Id" uuid NOT NULL,
        "HksIlId" integer NOT NULL,
        "Ad" text NOT NULL,
        "MusteriId" uuid NOT NULL,
        "AktifMi" boolean NOT NULL,
        "KayitTarihi" timestamp with time zone NOT NULL,
        "OlusturanKullanici" text,
        "GuncellemeTarihi" timestamp with time zone,
        "GuncelleyenKullanici" text,
        "SilinmeTarihi" timestamp with time zone,
        "SilenKullanici" text,
        "Source" integer NOT NULL,
        CONSTRAINT "PK_HksIller" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402121512_AddTenantScopedHksCities') THEN
    CREATE INDEX "IX_HksIl_SaaS_Performance" ON "HksIller" ("MusteriId", "AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402121512_AddTenantScopedHksCities') THEN
    CREATE UNIQUE INDEX "IX_HksIller_MusteriId_HksIlId_Unique" ON "HksIller" ("MusteriId", "HksIlId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402121512_AddTenantScopedHksCities') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260402121512_AddTenantScopedHksCities', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130046_ConvertHksCitiesToSharedDictionary') THEN
    DROP INDEX "IX_HksIl_SaaS_Performance";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130046_ConvertHksCitiesToSharedDictionary') THEN
    DROP INDEX "IX_HksIller_MusteriId_HksIlId_Unique";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130046_ConvertHksCitiesToSharedDictionary') THEN
    ALTER TABLE "HksIller" DROP COLUMN "MusteriId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130046_ConvertHksCitiesToSharedDictionary') THEN
    WITH deduplicated AS (
        SELECT
            ctid,
            ROW_NUMBER() OVER (
                PARTITION BY "HksIlId"
                ORDER BY
                    "AktifMi" DESC,
                    COALESCE("GuncellemeTarihi", "KayitTarihi") DESC,
                    "KayitTarihi" DESC,
                    "Id" DESC
            ) AS rn
        FROM "HksIller"
    )
    DELETE FROM "HksIller" target
    USING deduplicated source
    WHERE target.ctid = source.ctid
      AND source.rn > 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130046_ConvertHksCitiesToSharedDictionary') THEN
    CREATE INDEX "IX_HksIller_AktifMi_KayitTarihi" ON "HksIller" ("AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130046_ConvertHksCitiesToSharedDictionary') THEN
    CREATE UNIQUE INDEX "IX_HksIller_HksIlId_Unique" ON "HksIller" ("HksIlId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130046_ConvertHksCitiesToSharedDictionary') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260402130046_ConvertHksCitiesToSharedDictionary', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130902_ConvertHksProductsToSharedDictionary') THEN
    DROP INDEX "IX_HksUrun_SaaS_Performance";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130902_ConvertHksProductsToSharedDictionary') THEN
    DROP INDEX "IX_HksUrunler_MusteriId_HksUrunId_Unique";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130902_ConvertHksProductsToSharedDictionary') THEN
    ALTER TABLE "HksUrunler" DROP COLUMN "MusteriId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130902_ConvertHksProductsToSharedDictionary') THEN
    WITH deduplicated AS (
        SELECT
            ctid,
            ROW_NUMBER() OVER (
                PARTITION BY "HksUrunId"
                ORDER BY
                    "AktifMi" DESC,
                    COALESCE("GuncellemeTarihi", "KayitTarihi") DESC,
                    "KayitTarihi" DESC,
                    "Id" DESC
            ) AS rn
        FROM "HksUrunler"
    )
    DELETE FROM "HksUrunler" target
    USING deduplicated source
    WHERE target.ctid = source.ctid
      AND source.rn > 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130902_ConvertHksProductsToSharedDictionary') THEN
    CREATE INDEX "IX_HksUrunler_AktifMi_KayitTarihi" ON "HksUrunler" ("AktifMi", "KayitTarihi");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130902_ConvertHksProductsToSharedDictionary') THEN
    CREATE UNIQUE INDEX "IX_HksUrunler_HksUrunId_Unique" ON "HksUrunler" ("HksUrunId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402130902_ConvertHksProductsToSharedDictionary') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260402130902_ConvertHksProductsToSharedDictionary', '9.0.14');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402140558_AddCariCityField') THEN
    ALTER TABLE "CariKartlar" ADD "Il" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260402140558_AddCariCityField') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260402140558_AddCariCityField', '9.0.14');
    END IF;
END $EF$;
COMMIT;

