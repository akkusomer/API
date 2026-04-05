#!/usr/bin/env bash
set -euo pipefail

export PGPASSWORD='Omer.0742_'

echo '-- columns:FaturaDetaylari'
psql -h 127.0.0.1 -U postgres -d atlasweb_db -Atc "select column_name, data_type from information_schema.columns where table_schema='public' and table_name='FaturaDetaylari' order by ordinal_position;"

echo '-- migrations:20260405'
psql -h 127.0.0.1 -U postgres -d atlasweb_db -Atc 'select "MigrationId" from "__EFMigrationsHistory" where "MigrationId" like '"'"'20260405%'"'"' order by "MigrationId";'

echo '-- last10migrations'
psql -h 127.0.0.1 -U postgres -d atlasweb_db -Atc 'select "MigrationId" from "__EFMigrationsHistory" order by "MigrationId" desc limit 10;'
