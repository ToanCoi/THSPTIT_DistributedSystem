# THSPTIT_DistributedSystem — Triển khai trên minikube

Project PTIT Kỳ 2 — Hệ thống phân tán. 8 microservice `ecom/*` + MySQL (2 instance) + Kafka + ELK stack, chạy trên minikube local.

## Tài liệu

| Mục đích | File |
|---|---|
| **Hướng dẫn triển khai Kubernetes trên minikube** (yêu cầu, scripts, chart structure, troubleshooting) | [docs/03-deployment/k8s-deploy.md](../docs/03-deployment/k8s-deploy.md) |
| **Hướng dẫn build & run code local** (docker compose / dotnet run / Swagger) | [docs/03-deployment/local-dev.md](../docs/03-deployment/local-dev.md) |
| **Yêu cầu phần cứng & công cụ cần cài** | [docs/03-deployment/requirements.md](../docs/03-deployment/requirements.md) |
| **Catalog scripts triển khai** (cái nào dùng khi nào) | xem [docs/03-deployment/k8s-deploy.md § 3](../docs/03-deployment/k8s-deploy.md#3-script-catalog-9-scripts-trong-infrascripts) |
| **Kiến trúc tổng thể & service catalog** | [docs/02-architecture/](../docs/02-architecture/) |

## Nhanh nhất (1 lệnh)

Nếu máy đã có Docker Desktop + minikube + helm + kubectl:

```bash
git clone <repo-url> THSPTIT_DistributedSystem
cd THSPTIT_DistributedSystem
./infra/scripts/all.sh
```

Sau đó mở terminal mới, chạy `minikube -p ecom tunnel` (giữ mở), rồi truy cập **http://ecom.local/**.

## Cấu trúc

```
infra/
├── README.md                       ← file này (index)
├── scripts/                        ← 9 scripts triển khai (xem docs/03-deployment/k8s-deploy.md)
│   ├── all.sh                      ← master first-time
│   ├── daily-restart.sh            ← mỗi lần restart máy
│   ├── setup-minikube.sh
│   ├── build-images.sh
│   ├── install-nginx-ingress.sh
│   ├── install-elk.sh
│   ├── deploy-stack.sh
│   ├── seed-mysql.sh
│   └── teardown.sh
├── charts/                         ← Helm charts (ecom-stack umbrella + subcharts)
├── k8s/                            ← Raw k8s manifests (ingress, namespace)
└── logging/                        ← ELK stack config
```

> **Tài liệu hướng dẫn triển khai** đã được chuyển vào [`docs/03-deployment/`](../docs/03-deployment/) ở root project.
