#!/usr/bin/env bash
set -euo pipefail

release="/home/akkusomer0742/atlasweb-api/releases/api-202604050415"

run_sudo() {
  printf 'Omer.0742_\n' | sudo -S "$@"
}

rm -rf "$release"
mkdir -p "$release"
python3 -m zipfile -e /home/akkusomer0742/api-202604050415.zip "$release"
ln -sfn "$release" /home/akkusomer0742/atlasweb-api/current

run_sudo systemctl restart atlasweb-api.service

run_sudo rm -rf /var/www/atlasweb-templates/app
run_sudo mkdir -p /var/www/atlasweb-templates
run_sudo tar -xzf /home/akkusomer0742/templates-app-202604050415.tar.gz -C /var/www/atlasweb-templates
run_sudo chown -R www-data:www-data /var/www/atlasweb-templates || true

latest_js="$(find /var/www/atlasweb-templates/app/assets -maxdepth 1 -type f -name 'index-*.js' | sort | tail -n 1)"
latest_css="$(find /var/www/atlasweb-templates/app/assets -maxdepth 1 -type f -name 'index-*.css' | sort | tail -n 1)"

for alias in \
  /var/www/atlasweb-templates/app/assets/index-C_QGGnA5.js \
  /var/www/atlasweb-templates/app/assets/index-C_QG6nA5.js \
  /var/www/atlasweb-templates/app/assets/index-CPX05UFo.js \
  /var/www/atlasweb-templates/app/assets/index-D3h8BphF.js \
  /var/www/atlasweb-templates/app/assets/index-D-Sc2q4F.js \
  /var/www/atlasweb-templates/app/assets/index-BDh3MPuX.js \
  /var/www/atlasweb-templates/app/assets/index-JIRkREsh.js
do
  if [ -n "$latest_js" ]; then
    if [ "$latest_js" != "$alias" ]; then
      run_sudo cp "$latest_js" "$alias"
    fi
  fi
done

for alias in \
  /var/www/atlasweb-templates/app/assets/index-BD1LNtFp.css \
  /var/www/atlasweb-templates/app/assets/index-CtiVntrB.css
do
  if [ -n "$latest_css" ]; then
    if [ "$latest_css" != "$alias" ]; then
      run_sudo cp "$latest_css" "$alias"
    fi
  fi
done

systemctl is-active atlasweb-api.service
readlink -f /home/akkusomer0742/atlasweb-api/current
find /var/www/atlasweb-templates/app/assets -maxdepth 1 -type f | sort | tail -n 10
