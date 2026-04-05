#!/usr/bin/env bash
set -e
echo Omer.0742_ | sudo -S -u postgres psql -d atlasweb_db -Atc "SELECT current_database(), current_user;"
echo '---columns---'
echo Omer.0742_ | sudo -S -u postgres psql -d atlasweb_db -Atc "SELECT column_name FROM information_schema.columns WHERE table_schema='public' AND table_name='FaturaDetaylari' ORDER BY ordinal_position;"
echo '---migration---'
echo Omer.0742_ | sudo -S -u postgres psql -d atlasweb_db -Atc "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\"='20260405013000_AddInvoiceLineSalesKunye';"
