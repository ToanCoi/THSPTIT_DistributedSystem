#!/usr/bin/env bash
# Khởi động lại toàn bộ hệ thống ecom sau khi restart máy.
# Bao gồm: start minikube, verify pods, patch ingress-nginx LoadBalancer,
# kiểm tra hosts file, in trạng thái cuối.
#
# SAU KHI SCRIPT NÀY CHẠY XONG, cần mở terminal RIÊNG và chạy:
#   minikube -p ecom tunnel
# rồi mới truy cập được http://ecom.local/
set -euo pipefail

PROFILE=${PROFILE:-ecom}
NS_APP=${NS_APP:-ecom}

echo "[start-ecom] Bắt đầu khởi động lại hệ thống (profile=${PROFILE})"

# 1. Start minikube nếu chưa chạy
if ! minikube status -p "${PROFILE}" >/dev/null 2>&1; then
  echo "[start-ecom] Cluster '${PROFILE}' chưa chạy → start..."
  minikube start \
    --profile="${PROFILE}" \
    --driver=docker \
    --cpus=4 \
    --memory=10g \
    --disk-size=30g \
    --addons=ingress \
    --addons=metrics-server \
    --addons=storage-provisioner
else
  echo "[start-ecom] Cluster '${PROFILE}' đã chạy."
fi

# 2. Đợi node ready
echo "[start-ecom] Đợi node ready..."
kubectl wait --for=condition=Ready nodes --all --timeout=120s

# 3. Kiểm tra pods (đợi nếu chưa ready)
echo "[start-ecom] Trạng thái pods:"
kubectl -n "${NS_APP}" get pods
NOT_READY=$(kubectl -n "${NS_APP}" get pods --no-headers 2>/dev/null | awk '$2 !~ /^[0-9]+\/[0-9]+$/ {print}' | wc -l)
if [ "${NOT_READY}" -gt 0 ]; then
  echo "[start-ecom] Có pod chưa ready. Đợi 60s..."
  sleep 60
  kubectl -n "${NS_APP}" get pods
fi

# 4. Patch ingress-nginx service → LoadBalancer (cần cho minikube tunnel expose port 80)
SVC_TYPE=$(kubectl -n ingress-nginx get svc ingress-nginx-controller -o jsonpath='{.spec.type}' 2>/dev/null || echo "Missing")
if [ "${SVC_TYPE}" != "LoadBalancer" ]; then
  echo "[start-ecom] Patch ingress-nginx-controller → LoadBalancer"
  kubectl -n ingress-nginx patch svc ingress-nginx-controller -p '{"spec":{"type":"LoadBalancer"}}' >/dev/null
  sleep 3
else
  echo "[start-ecom] ingress-nginx-controller đã là LoadBalancer."
fi

# 5. Kiểm tra hosts file
echo ""
echo "[start-ecom] Kiểm tra hosts file:"
HOSTS_FILE="C:/Windows/System32/drivers/etc/hosts"
if grep -q "ecom.local" "${HOSTS_FILE}" 2>/dev/null; then
  CURRENT=$(grep "ecom.local" "${HOSTS_FILE}" | head -1 | tr -d '\r')
  echo "  ${CURRENT}"
  if [[ "${CURRENT}" != *"127.0.0.1"* ]]; then
    echo "  ⚠️  hosts trỏ sai IP. Cần fix (PowerShell Admin):"
    echo "  \$h = 'C:\Windows\System32\drivers\etc\hosts'"
    echo "  (Get-Content \$h) -replace '\d+\.\d+\.\d+\.\d+ ecom\.local', '127.0.0.1 ecom.local' | Set-Content \$h"
    echo "  ipconfig /flushdns"
  fi
else
  echo "  ⚠️  hosts chưa có ecom.local. Cần thêm (PowerShell Admin):"
  echo "  Add-Content C:\Windows\System32\drivers\etc\hosts '127.0.0.1 ecom.local'"
  echo "  ipconfig /flushdns"
fi

# 6. In trạng thái cuối
echo ""
echo "[start-ecom] ================================================"
echo "[start-ecom] Trạng thái cuối:"
echo "[start-ecom] ================================================"
echo "Pods (namespace ecom):"
kubectl -n "${NS_APP}" get pods
echo ""
echo "Ingress:"
kubectl -n "${NS_APP}" get ingress ecom
echo ""
echo "ingress-nginx controller:"
kubectl -n ingress-nginx get svc ingress-nginx-controller
echo ""
echo "[start-ecom] ✅ Setup xong."
echo "[start-ecom] 👉 BƯỚC CUỐI: mở terminal RIÊNG và chạy:"
echo ""
echo "       minikube -p ${PROFILE} tunnel"
echo ""
echo "[start-ecom] Sau đó browser: http://ecom.local/"
echo "[start-ecom] Lưu ý: nếu browser báo 503, đợi 5-10s rồi refresh (tunnel cần thời gian apply route)."
