#!/usr/bin/env bash
set -euo pipefail

export PGPASSWORD='Omer.0742_'

psql -h 127.0.0.1 -U postgres -d atlasweb_db <<'SQL'
ALTER TABLE "FaturaDetaylari"
ADD COLUMN IF NOT EXISTS "SatisKunye" character varying(120);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260405013000_AddInvoiceLineSalesKunye', '9.0.14'
WHERE NOT EXISTS (
    SELECT 1
    FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260405013000_AddInvoiceLineSalesKunye'
);
SQL

psql -h 127.0.0.1 -U postgres -d atlasweb_db -Atc "select column_name from information_schema.columns where table_schema='public' and table_name='FaturaDetaylari' and column_name='SatisKunye';"
psql -h 127.0.0.1 -U postgres -d atlasweb_db -Atc "select \"MigrationId\" from \"__EFMigrationsHistory\" where \"MigrationId\"='20260405013000_AddInvoiceLineSalesKunye';"
