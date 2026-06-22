# Catalog scripts triển khai

Tất cả scripts nằm trong `infra/scripts/`. Mỗi script làm 1 việc cụ thể, idempotent nếu có thể.

## Tổng quan

| Script | Mục đích | Khi nào dùng | Phụ thuộc |
|---|---|---|---|
| `all.sh` | Chạy tất cả first-time theo thứ tự | Lần đầu setup máy mới | Docker, minikube, helm, kubectl |
| `setup-minikube.sh` | Tạo cluster `ecom` với addons (ingress, metrics-server, storage-provisioner) | Lần đầu, hoặc sau khi `minikube delete` | Docker Desktop |
| `build-images.sh` | Build 8 image `ecom/*` đẩy vào minikube daemon | Sau khi đổi code, hoặc lần đầu | `setup-minikube.sh` (cần `docker-env`) |
| `install-nginx-ingress.sh` | Cài nginx ingress thủ công (legacy) | Thường không cần — addon đã có từ `setup-minikube` | `setup-minikube.sh` |
| `install-elk.sh` | Cài ELK (Elasticsearch, Kibana, Logstash, Filebeat) vào namespace `logging` | Sau `setup-minikube` | `setup-minikube.sh` |
| `deploy-stack.sh` | Helm install umbrella chart `ecom-stack` vào namespace `ecom` | Sau `build-images` | `build-images.sh` |
| `seed-mysql.sh` | Tạo schema + data mẫu trong MySQL | Sau `deploy-stack` (khi MySQL ready) | `deploy-stack.sh` |
| `daily-restart.sh` | Khởi động lại sau khi restart máy: start minikube, patch ingress, check hosts | Mỗi lần restart PC | Cluster đã tồn tại |
| `teardown.sh` | Xoá Helm release + namespace `ecom` + `logging` | Khi muốn dọn dẹp, giữ cluster | – |

## Sơ đồ phụ thuộc

```
all.sh
 ├─→ setup-minikube.sh          (tạo cluster)
 ├─→ build-images.sh            (cần docker-env từ setup)
 │     └─→ docker build 8 image
 ├─→ install-nginx-ingress.sh   (legacy, thường skip vì đã có addon)
 ├─→ install-elk.sh             (cài logging stack)
 ├─→ deploy-stack.sh            (helm install ecom-stack)
 └─→ seed-mysql.sh              (optional, hỏi user)

daily-restart.sh                (chạy SAU khi cluster đã có)
teardown.sh                     (chạy khi muốn reset)
```

## Lệnh nhanh theo tình huống

| Tình huống | Chạy |
|---|---|
| Máy mới, chưa có gì | `./infra/scripts/all.sh` |
| Đổi code backend/frontend | `eval $(minikube -p ecom docker-env) && ./infra/scripts/build-images.sh && kubectl -n ecom rollout restart deployment <name>` |
| Restart máy xong | `./infra/scripts/daily-restart.sh` (mở terminal riêng chạy `minikube -p ecom tunnel`) |
| Chỉ tunnel chết | `minikube -p ecom tunnel` (terminal riêng) |
| Muốn xoá sạch và làm lại | `./infra/scripts/teardown.sh && minikube -p ecom delete` rồi `./infra/scripts/all.sh` |

## Convention

- Tất cả scripts dùng `set -euo pipefail` → fail-fast nếu lỗi
- Log prefix theo tên script: `[setup-minikube]`, `[build-images]`, etc.
- Env vars có default: `NS_APP=ecom`, `NS_LOG=logging`, `PROFILE=ecom`, `TAG=v1.0.0-local`
- Namespace `ingress-nginx` tự tạo khi chạy `setup-minikube.sh` (qua addon)
- Namespace `kubernetes-dashboard` tự tạo khi chạy `minikube dashboard`

## Liên quan

- Hướng dẫn lần đầu: [02-first-time-setup.md](02-first-time-setup.md)
- Hướng dẫn mỗi lần mở lại: [03-daily-restart.md](03-daily-restart.md)
- Trạng thái hệ thống sau khi chạy: [04-system-overview.md](04-system-overview.md)
- Troubleshooting: [05-troubleshooting.md](05-troubleshooting.md)
