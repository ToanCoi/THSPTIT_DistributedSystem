#!/usr/bin/env bash
# Master first-time setup script.
# Chạy tất cả theo thứ tự: setup cluster → build images → install ingress → ELK → deploy → seed.
# Idempotent: skip bước đã chạy nếu có thể.
#
# Điều kiện: Docker Desktop đang chạy, đã cài minikube + kubectl + helm.
# Xem docs/02-first-time-setup.md để biết chi tiết.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "[all] ============================================="
echo "[all] ecom-stack master setup"
echo "[all] Profile: ${PROFILE:-ecom}"
echo "[all] ============================================="

# 1. Setup minikube cluster
echo ""
echo "[all] === 1/6: Setup minikube cluster ==="
bash "${SCRIPT_DIR}/setup-minikube.sh"

# 2. Build Docker images (cần docker-env đã set từ setup-minikube)
echo ""
echo "[all] === 2/6: Build 8 Docker images ==="
bash "${SCRIPT_DIR}/build-images.sh"

# 3. Install nginx ingress (legacy — addon từ setup-minikube thường đã đủ)
echo ""
echo "[all] === 3/6: Install nginx ingress (legacy, nếu chưa có) ==="
if ! kubectl get ns ingress-nginx >/dev/null 2>&1; then
  bash "${SCRIPT_DIR}/install-nginx-ingress.sh"
else
  echo "[all] ingress-nginx namespace đã tồn tại, skip."
fi

# 4. Install ELK stack
echo ""
echo "[all] === 4/6: Install ELK stack ==="
bash "${SCRIPT_DIR}/install-elk.sh"

# 5. Deploy ecom-stack
echo ""
echo "[all] === 5/6: Deploy ecom-stack ==="
bash "${SCRIPT_DIR}/deploy-stack.sh"

# 6. Seed MySQL (optional)
echo ""
echo "[all] === 6/6: Seed MySQL (tuỳ chọn) ==="
read -rp "Seed MySQL data? [y/N] " ans
if [[ "${ans}" =~ ^[Yy]$ ]]; then
  bash "${SCRIPT_DIR}/seed-mysql.sh"
else
  echo "[all] Bỏ qua seed MySQL."
fi

echo ""
echo "[all] ============================================="
echo "[all] ✅ Hoàn tất first-time setup!"
echo "[all] ============================================="
echo ""
echo "[all] Bước tiếp theo:"
echo "[all]   1. Mở terminal MỚI và chạy:"
echo "[all]        minikube -p ${PROFILE:-ecom} tunnel"
echo "[all]      (giữ terminal này mở, Ctrl+C mới tắt)"
echo ""
echo "[all]   2. PowerShell Admin (1 lần):"
echo "[all]        Add-Content C:\Windows\System32\drivers\etc\hosts '127.0.0.1 ecom.local'"
echo "[all]        ipconfig /flushdns"
echo ""
echo "[all]   3. Browser: http://ecom.local/"
echo ""
echo "[all] Lỗi? Xem docs/05-troubleshooting.md"
