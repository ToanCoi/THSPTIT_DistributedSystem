#!/usr/bin/env bash
# Cài nginx-ingress controller. Có thể bỏ qua nếu đã enable addon 'ingress' ở setup-minikube.
set -euo pipefail

PROFILE=${PROFILE:-ecom}
NS=${NS:-ingress-nginx}

echo "[install-nginx-ingress] Cài nginx-ingress vào namespace '${NS}'."

if ! command -v helm >/dev/null 2>&1; then
  echo "[install-nginx-ingress] Lỗi: chưa cài helm. Cài qua: choco install kubernetes-helm."
  exit 1
fi

helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx >/dev/null 2>&1 || true
helm repo update

if ! kubectl get ns "${NS}" >/dev/null 2>&1; then
  kubectl create namespace "${NS}"
fi

# Trên minikube, addon ingress có thể đã có controller. Check trước khi cài Helm.
if kubectl get ns ingress-nginx >/dev/null 2>&1 && kubectl -n ingress-nginx get deploy ingress-nginx-controller >/dev/null 2>&1; then
  echo "[install-nginx-ingress] ingress-nginx controller đã tồn tại (do addon minikube). Bỏ qua helm install."
else
  helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
    --namespace "${NS}" \
    --set controller.service.type=NodePort \
    --set controller.service.nodePorts.http=30080 \
    --set controller.service.nodePorts.https=30443 \
    --set controller.ingressClassResource.default=true \
    --wait
fi

echo "[install-nginx-ingress] Đợi controller ready..."
kubectl -n "${NS}" wait --for=condition=Available --timeout=120s deploy/ingress-nginx-controller || true
kubectl -n "${NS}" get pods
echo "[install-nginx-ingress] Hoàn tất."
