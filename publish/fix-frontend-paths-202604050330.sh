#!/usr/bin/env bash
set -euo pipefail

run_sudo() {
  printf 'Omer.0742_\n' | sudo -S "$@"
}

run_sudo mkdir -p /var/www/atlasweb-templates/app/assets
run_sudo mv '/var/www/atlasweb-templates/app/assets\index-BD1LNtFp.css' /var/www/atlasweb-templates/app/assets/index-BD1LNtFp.css
run_sudo mv '/var/www/atlasweb-templates/app/assets\index-D-Sc2q4F.js' /var/www/atlasweb-templates/app/assets/index-D-Sc2q4F.js
find /var/www/atlasweb-templates/app -maxdepth 2 -type f
