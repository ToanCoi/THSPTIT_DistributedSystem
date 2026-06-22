#!/usr/bin/env bash
# Import init.sql vào mysql-master (sau khi pod chạy).
# Schema master_db có user/role; business_db có customer/product/stock/inward/outward/ledger/order.
set -euo pipefail

NS_APP=${NS_APP:-ecom}

echo "[seed-mysql] Tìm pod mysql-master..."
MYSQL_POD=$(kubectl -n "${NS_APP}" get pod -l app=mysql-master -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || true)

if [ -z "${MYSQL_POD}" ]; then
  echo "[seed-mysql] Không tìm thấy mysql-master pod. Hãy chạy deploy-stack.sh trước."
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INIT_SQL="${SCRIPT_DIR}/../../backend/scripts/init.sql"

if [ ! -f "${INIT_SQL}" ]; then
  echo "[seed-mysql] Không tìm thấy ${INIT_SQL}. Bỏ qua seeding."
  exit 0
fi

echo "[seed-mysql] Import ${INIT_SQL} vào ${MYSQL_POD}..."
kubectl -n "${NS_APP}" exec -i "${MYSQL_POD}" -- mysql -uroot -p"${MYSQL_ROOT_PASSWORD:-P@ssw0rd123}" < "${INIT_SQL}" || true

# Re-import cho business_db
kubectl -n "${NS_APP}" exec -i "${MYSQL_POD}" -- sh -c "mysql -uroot -p\"${MYSQL_ROOT_PASSWORD:-P@ssw0rd123}\" -e 'USE business_db; SHOW TABLES;'"

echo "[seed-mysql] Hoàn tất."
