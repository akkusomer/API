#!/usr/bin/env bash
set -euo pipefail

release="/home/akkusomer0742/atlasweb-api/releases/api-202604050450"

run_sudo() {
  printf 'Omer.0742_\n' | sudo -S "$@"
}

run_sudo rm -rf "$release"
mkdir -p "$release"
python3 -m zipfile -e /home/akkusomer0742/api-202604050450.zip "$release"
ln -sfn "$release" /home/akkusomer0742/atlasweb-api/current
run_sudo systemctl restart atlasweb-api.service
systemctl is-active atlasweb-api.service
readlink -f /home/akkusomer0742/atlasweb-api/current
