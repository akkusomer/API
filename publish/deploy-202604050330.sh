#!/usr/bin/env bash
set -euo pipefail

release="/home/akkusomer0742/atlasweb-api/releases/api-202604050330"

run_sudo() {
  printf 'Omer.0742_\n' | sudo -S "$@"
}

rm -rf "$release"
mkdir -p "$release"
python3 -m zipfile -e /home/akkusomer0742/api-202604050330.zip "$release"
ln -sfn "$release" /home/akkusomer0742/atlasweb-api/current

run_sudo systemctl restart atlasweb-api.service

run_sudo mkdir -p /var/www/atlasweb-templates/app
run_sudo rm -rf /var/www/atlasweb-templates/app/*
run_sudo python3 -m zipfile -e /home/akkusomer0742/app-202604050330.zip /var/www/atlasweb-templates/app
run_sudo chown -R www-data:www-data /var/www/atlasweb-templates || true

systemctl is-active atlasweb-api.service
readlink -f /home/akkusomer0742/atlasweb-api/current
ls /var/www/atlasweb-templates/app/assets | tail -n 5
