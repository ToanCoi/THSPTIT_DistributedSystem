#!/usr/bin/env bash
# Xoá toàn bộ stack: uninstall Helm releases, xoá namespace ecom + logging.
set -euo pipefail

NS_APP=${NS_APP:-ecom}
NS_LOG=${NS_LOG:-logging}

echo "[teardown] Bắt đầu gỡ stack..."

echo "[teardown] helm uninstall ecom-stack..."
helm uninstall ecom-stack -n "${NS_APP}" 2>/dev/null || true

echo "[teardown] helm uninstall filebeat/logstash/kibana/elasticsearch..."
helm uninstall filebeat     -n "${NS_LOG}" 2>/dev/null || true
helm uninstall logstash     -n "${NS_LOG}" 2>/dev/null || true
helm uninstall kibana       -n "${NS_LOG}" 2>/dev/null || true
helm uninstall elasticsearch -n "${NS_LOG}" 2>/dev/null || true

echo "[teardown] Xoá namespace ${NS_APP} và ${NS_LOG}..."
kubectl delete namespace "${NS_APP}" --ignore-not-found
kubectl delete namespace "${NS_LOG}" --ignore-not-found

echo "[teardown] Hoàn tất."
