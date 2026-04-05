#!/usr/bin/env bash
set -euo pipefail

run_sudo() {
  printf 'Omer.0742_\n' | sudo -S "$@"
}

run_sudo rm -rf /var/www/atlasweb-templates/app
run_sudo mkdir -p /var/www/atlasweb-templates
run_sudo tar -xzf /home/akkusomer0742/templates-app-202604050345.tar.gz -C /var/www/atlasweb-templates
run_sudo chown -R www-data:www-data /var/www/atlasweb-templates || true

find /var/www/atlasweb-templates/app/assets -maxdepth 1 -type f | sort
