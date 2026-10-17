#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
image_tag="${KRIZOVKA_IMAGE_TAG:-bootstrap-20261017}"
mkdir -p .local
build() {
  local service="$1" dockerfile="$2"
  echo "Building krizovka/$service:$image_tag"
  if ! docker build -f "$dockerfile" -t "krizovka/$service:$image_tag" . >".local/build-$service.log" 2>&1; then
    tail -n 60 ".local/build-$service.log"
    return 1
  fi
  echo "Built krizovka/$service:$image_tag"
}
build api backend/Krizovka.Api/Dockerfile
build users backend/recycled/src/Services/US/Dockerfile
build admin apps/admin/Dockerfile
build client apps/client/Dockerfile
