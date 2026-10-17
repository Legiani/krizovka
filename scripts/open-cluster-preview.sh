#!/usr/bin/env bash
set -euo pipefail
cluster_host="${KRIZOVKA_SSH_HOST:-kuba@192.168.1.38}"

echo "Flutter: http://127.0.0.1:3080 | Velín: http://127.0.0.1:3100"
echo "Preview ends with Ctrl+C."
# The two forwards run for this SSH session and listen on server loopback only.
ssh -o ExitOnForwardFailure=yes \
  -L 127.0.0.1:3080:127.0.0.1:3080 \
  -L 127.0.0.1:3100:127.0.0.1:3100 \
  "$cluster_host" 'bash -s' <<'REMOTE'
set -euo pipefail
kubectl -n krizovka port-forward --address 127.0.0.1 service/krizovka-client 3080:80 &
client_pid=$!
kubectl -n krizovka port-forward --address 127.0.0.1 service/krizovka-admin 3100:3000 &
admin_pid=$!
trap 'kill "$client_pid" "$admin_pid" 2>/dev/null || true' EXIT HUP INT TERM
wait -n "$client_pid" "$admin_pid"
REMOTE
