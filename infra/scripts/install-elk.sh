#!/usr/bin/env bash
# Cài ELK stack (Elasticsearch + Kibana + Logstash + Filebeat) vào namespace 'logging'.
# Dùng chart upstream elastic/* với values override cho local (heap nhỏ, replica ít).
set -euo pipefail

NS_LOG=${NS_LOG:-logging}

echo "[install-elk] Cài ELK vào namespace '${NS_LOG}'."

if ! command -v helm >/dev/null 2>&1; then
  echo "[install-elk] Lỗi: chưa cài helm."
  exit 1
fi

if ! kubectl get ns "${NS_LOG}" >/dev/null 2>&1; then
  kubectl create namespace "${NS_LOG}"
fi

helm repo add elastic https://helm.elastic.co >/dev/null 2>&1 || true
helm repo update

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOGGING_VALUES_DIR="${SCRIPT_DIR}/../logging"

# --- Elasticsearch ---
echo "[install-elk] Cài Elasticsearch (1 replica, heap 512m)..."
helm upgrade --install elasticsearch elastic/elasticsearch \
  --namespace "${NS_LOG}" \
  -f "${LOGGING_VALUES_DIR}/elasticsearch-values.yaml" \
  --wait --timeout 5m

# --- Kibana ---
echo "[install-elk] Cài Kibana..."
helm upgrade --install kibana elastic/kibana \
  --namespace "${NS_LOG}" \
  -f "${LOGGING_VALUES_DIR}/kibana-values.yaml" \
  --wait --timeout 5m

# --- Logstash ---
echo "[install-elk] Cài Logstash..."
helm upgrade --install logstash elastic/logstash \
  --namespace "${NS_LOG}" \
  -f "${LOGGING_VALUES_DIR}/logstash-values.yaml" \
  --wait --timeout 5m

# --- Filebeat (DaemonSet) ---
echo "[install-elk] Cài Filebeat (DaemonSet)..."
helm upgrade --install filebeat elastic/filebeat \
  --namespace "${NS_LOG}" \
  -f "${LOGGING_VALUES_DIR}/filebeat-values.yaml" \
  --wait --timeout 5m

echo "[install-elk] Hoàn tất. Trạng thái pods:"
kubectl -n "${NS_LOG}" get pods
echo "[install-elk] Kibana sẽ được expose qua ingress sau khi ecom-stack deploy."
