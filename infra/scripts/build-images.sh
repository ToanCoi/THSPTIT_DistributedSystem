#!/usr/bin/env bash
# Build Docker images cho 7 backend services + 1 frontend.
# Cần chạy SAU setup-minikube.sh (để docker-env trỏ vào minikube daemon).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
TAG=${TAG:-v1.0.0-local}

echo "[build-images] Build context = ${ROOT_DIR}"
echo "[build-images] Tag = ${TAG}"

build() {
  local name="$1"
  local context="$2"
  local dockerfile="$3"
  echo "[build-images] Building ${name}:${TAG} (context=${context}, dockerfile=${dockerfile})"
  docker build \
    -t "ecom/${name}:${TAG}" \
    -f "${ROOT_DIR}/${dockerfile}" \
    "${ROOT_DIR}/${context}"
}

# Backend services (đã có Dockerfile sẵn)
build api-gateway     backend  backend/ApiGateway/Dockerfile
build auth-api        backend  backend/AuthApi/Dockerfile
build business-api    backend  backend/BusinessApi/Dockerfile
build order-api       backend  backend/OrderApi/Dockerfile
build handle-worker   backend  backend/HandleWorker/Dockerfile
build ledger-worker   backend  backend/Workers/LedgerWorker/Dockerfile
build voucher-worker  backend  backend/Workers/VoucherWorker/Dockerfile

# Frontend (Dockerfile mới)
build frontend        frontend frontend/Dockerfile

echo "[build-images] Hoàn tất. Danh sách images:"
docker images | grep -E "ecom/(api-gateway|auth-api|business-api|order-api|handle-worker|ledger-worker|voucher-worker|frontend)"
