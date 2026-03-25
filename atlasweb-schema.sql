-- =============================================================================
-- AtlasWeb — PostgreSQL şema (DDL)
-- Kaynak: Models + DbContext; yeni kurulum veya referans için.
-- Uyarı: Mevcut veritabanınızda tablolar varsa önce yedek alın; isimler EF ile
-- uyumlu olmalıdır (büyük/küçük harf: çift tırnaklı tanımlar).
-- =============================================================================

-- Gerekirse şema:
-- CREATE SCHEMA IF NOT EXISTS public;

-- -----------------------------------------------------------------------------
-- Musteriler (şirket / kiracı)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Musteriler" (
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
    "AktifMi" boolean NOT NULL DEFAULT TRUE,
    "KayitTarihi" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Musteriler" PRIMARY KEY ("Id")
);

-- -----------------------------------------------------------------------------
-- Birimler (global ölçü birimi; kiracı FK’si yok)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Birimler" (
    "Id" uuid NOT NULL,
    "Ad" text NOT NULL,
    "Sembol" text NOT NULL,
    "AktifMi" boolean NOT NULL DEFAULT TRUE,
    "SilinmeTarihi" timestamp with time zone NULL,
    "SilenKullanici" text NULL,
    "KayitTarihi" timestamp with time zone NOT NULL,
    "OlusturanKullanici" text NULL,
    "GuncellemeTarihi" timestamp with time zone NULL,
    "GuncelleyenKullanici" text NULL,
    "Source" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_Birimler" PRIMARY KEY ("Id")
);

-- -----------------------------------------------------------------------------
-- Kullanicilar
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Kullanicilar" (
    "Id" uuid NOT NULL,
    "Ad" character varying(100) NOT NULL,
    "Soyad" character varying(100) NOT NULL,
    "EPosta" text NOT NULL,
    "SifreHash" text NOT NULL,
    "Rol" text NOT NULL,
    "MusteriId" uuid NOT NULL,
    "AktifMi" boolean NOT NULL DEFAULT TRUE,
    "KayitTarihi" timestamp with time zone NOT NULL,
    "RefreshToken" text NULL,
    "RefreshTokenExpiryTime" timestamp with time zone NULL,
    "OlusturanKullanici" text NULL,
    "GuncellemeTarihi" timestamp with time zone NULL,
    "GuncelleyenKullanici" text NULL,
    "SilinmeTarihi" timestamp with time zone NULL,
    "SilenKullanici" text NULL,
    "Source" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_Kullanicilar" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Kullanicilar_Musteriler_MusteriId"
        FOREIGN KEY ("MusteriId") REFERENCES "Musteriler" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_Kullanici_SaaS_Performance"
    ON "Kullanicilar" ("MusteriId", "AktifMi", "KayitTarihi");

-- -----------------------------------------------------------------------------
-- Faturalar (Id: kodda uuid; eski DB’de int ise migration ile dönüştürün)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Faturalar" (
    "Id" uuid NOT NULL,
    "MusteriId" uuid NOT NULL,
    "AktifMi" boolean NOT NULL DEFAULT TRUE,
    "KayitTarihi" timestamp with time zone NOT NULL,
    "FaturaNo" text NOT NULL,
    "Tutar" numeric NOT NULL,
    "OlusturanKullanici" text NULL,
    "GuncellemeTarihi" timestamp with time zone NULL,
    "GuncelleyenKullanici" text NULL,
    "SilinmeTarihi" timestamp with time zone NULL,
    "SilenKullanici" text NULL,
    "Source" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_Faturalar" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Faturalar_Musteriler_MusteriId"
        FOREIGN KEY ("MusteriId") REFERENCES "Musteriler" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_Fatura_SaaS_Performance"
    ON "Faturalar" ("MusteriId", "AktifMi", "KayitTarihi");

-- -----------------------------------------------------------------------------
-- Stoklar
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Stoklar" (
    "Id" uuid NOT NULL,
    "MusteriId" uuid NOT NULL,
    "AktifMi" boolean NOT NULL DEFAULT TRUE,
    "KayitTarihi" timestamp with time zone NOT NULL,
    "StokKodu" text NOT NULL,
    "StokAdi" text NOT NULL,
    "YedekAdi" text NULL,
    "BirimId" uuid NOT NULL,
    "OlusturanKullanici" text NULL,
    "GuncellemeTarihi" timestamp with time zone NULL,
    "GuncelleyenKullanici" text NULL,
    "SilinmeTarihi" timestamp with time zone NULL,
    "SilenKullanici" text NULL,
    "Source" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_Stoklar" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Stoklar_Musteriler_MusteriId"
        FOREIGN KEY ("MusteriId") REFERENCES "Musteriler" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Stoklar_Birimler_BirimId"
        FOREIGN KEY ("BirimId") REFERENCES "Birimler" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_Stok_SaaS_Performance"
    ON "Stoklar" ("MusteriId", "AktifMi", "KayitTarihi");

-- -----------------------------------------------------------------------------
-- AuditLogs
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" uuid NOT NULL,
    "EntityName" text NOT NULL,
    "EntityId" text NOT NULL,
    "Action" text NOT NULL,
    "UserId" text NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
);

-- -----------------------------------------------------------------------------
-- ErrorLogs
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "ErrorLogs" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "HataMesaji" text NOT NULL,
    "HataDetayi" text NULL,
    "IstekYolu" text NULL,
    "KullaniciId" text NULL,
    "Tarih" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ErrorLogs" PRIMARY KEY ("Id")
);

-- =============================================================================
-- Bitiş
-- =============================================================================
