#!/usr/bin/env bash
# Build dependency + deploy umbrella chart ecom-stack vào namespace 'ecom'.
# Bao gồm: api-gateway, auth-api, business-api, order-api, 3 workers, frontend, ingress.
set -euo pipefail

NS_APP=${NS_APP:-ecom}
TAG=${TAG:-v1.0.0-local}

echo "[deploy-stack] Deploy ecom-stack vào namespace '${NS_APP}' với image tag '${TAG}'."

if ! kubectl get ns "${NS_APP}" >/dev/null 2>&1; then
  kubectl create namespace "${NS_APP}"
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_INFRA="${SCRIPT_DIR}/.."

# Update chart dependencies
echo "[deploy-stack] helm dependency update ecom-stack..."
helm dependency update "${ROOT_INFRA}/charts/ecom-stack"

helm upgrade --install ecom-stack "${ROOT_INFRA}/charts/ecom-stack" \
  --namespace "${NS_APP}" \
  --set global.imageTag="${TAG}" \
  --wait --timeout 8m

# Kafka: Bitnami chart đã tắt trong umbrella (xem values.yaml).
# Deploy thủ công bằng apache/kafka:3.8.1 (KRaft mode) — image này còn trên Docker Hub,
# trong khi bitnami/kafka đã bị gỡ khỏi free tier (late 2025).
echo "[deploy-stack] Apply custom Kafka (apache/kafka:3.8.1)..."
kubectl apply -n "${NS_APP}" -f "${ROOT_INFRA}/k8s/kafka.yaml"

echo "[deploy-stack] Hoàn tất. Trạng thái pods:"
kubectl -n "${NS_APP}" get pods
echo "[deploy-stack] Ingress:"
kubectl -n "${NS_APP}" get ingress
