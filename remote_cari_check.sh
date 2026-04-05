#!/usr/bin/env bash
set -e
echo Omer.0742_ | sudo -S -u postgres psql -d atlasweb_db -c "SELECT \"Id\", \"Unvan\", \"AdiSoyadi\", \"VTCK_No\", \"HksSifatId\", \"HksIsletmeTuruId\", \"HksHalIciIsyeriId\", \"HalIciIsyeriAdi\", \"HksIlId\", \"HksIlceId\", \"HksBeldeId\" FROM \"CariKartlar\" WHERE \"AktifMi\" ORDER BY COALESCE(\"GuncellemeTarihi\", \"KayitTarihi\") DESC LIMIT 10;"
