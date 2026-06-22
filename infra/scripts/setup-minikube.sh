#!/usr/bin/env bash
# Khởi tạo minikube cluster với driver=docker, kích hoạt ingress + metrics-server.
# Cần: Docker Desktop đang chạy, hypervisor enabled trên Windows.
set -euo pipefail

CPU=${CPU:-4}
MEMORY=${MEMORY:-10g}
DISK=${DISK:-30g}
DRIVER=${DRIVER:-docker}
PROFILE=${PROFILE:-ecom}

echo "[setup-minikube] Bắt đầu khởi tạo minikube (profile=${PROFILE}, driver=${DRIVER}, cpu=${CPU}, mem=${MEMORY}, disk=${DISK})"

if ! command -v minikube >/dev/null 2>&1; then
  echo "[setup-minikube] Lỗi: chưa cài minikube. Cài qua: choco install minikube (Windows) hoặc brew install minikube (macOS)."
  exit 1
fi

if ! minikube status -p "${PROFILE}" >/dev/null 2>&1; then
  minikube start \
    --profile="${PROFILE}" \
    --driver="${DRIVER}" \
    --cpus="${CPU}" \
    --memory="${MEMORY}" \
    --disk-size="${DISK}" \
    --addons=ingress \
    --addons=metrics-server \
    --addons=storage-provisioner
else
  echo "[setup-minikube] Cluster '${PROFILE}' đã tồn tại, bỏ qua start."
fi

minikube profile "${PROFILE}"

eval "$(minikube docker-env -p "${PROFILE}")"
echo "[setup-minikube] Đã set DOCKER_HOST trỏ vào Docker daemon của minikube."

kubectl get nodes
echo "[setup-minikube] Hoàn tất. Cluster ready."
