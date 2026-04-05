ALTER TABLE "FaturaDetaylari"
ADD COLUMN IF NOT EXISTS "SatisKunye" character varying(120);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260405013000_AddInvoiceLineSalesKunye', '9.0.14'
WHERE NOT EXISTS (
    SELECT 1
    FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260405013000_AddInvoiceLineSalesKunye'
);
