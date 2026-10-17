#!/usr/bin/env bash
set -euo pipefail
cluster_host="${KRIZOVKA_SSH_HOST:-kuba@192.168.1.38}"

echo "=========================================================="
echo "  Krizovka Cluster Preview"
echo "  - Klientská aplikace (App): http://127.0.0.1:3810"
echo "  - Backend API:             http://127.0.0.1:3811"
echo "  - Krizový Velín (BO):       http://127.0.0.1:3812"
echo "=========================================================="
echo "Ukončení stisknutím Ctrl+C."

exec ssh -o ExitOnForwardFailure=yes -N \
  -L 127.0.0.1:3810:127.0.0.1:3810 \
  -L 127.0.0.1:3811:127.0.0.1:3811 \
  -L 127.0.0.1:3812:127.0.0.1:3812 \
  "$cluster_host"
