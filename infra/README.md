# THSPTIT_DistributedSystem — Triển khai trên minikube

Project PTIT Kỳ 2 — Hệ thống phân tán. 8 microservice `ecom/*` + MySQL (2 instance) + Kafka + ELK stack, chạy trên minikube local.

## Tài liệu

| Mục đích | File |
|---|---|
| **Cài đặt & chạy lần đầu** (máy mới, chưa có gì) | [docs/02-first-time-setup.md](docs/02-first-time-setup.md) |
| **Mỗi lần mở lại máy** (restart PC, tunnel chết) | [docs/03-daily-restart.md](docs/03-daily-restart.md) |
| **Trạng thái hệ thống** (kiến trúc, URLs, services, namespace) | [docs/04-system-overview.md](docs/04-system-overview.md) |
| **Catalog scripts triển khai** (cái nào dùng khi nào) | [docs/01-deploy-scripts.md](docs/01-deploy-scripts.md) |
| **Troubleshooting** (lỗi thường gặp + fix) | [docs/05-troubleshooting.md](docs/05-troubleshooting.md) |

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
├── docs/                           ← 5 tài liệu triển khai
└── scripts/                        ← 9 scripts triển khai (xem docs/01)
    ├── all.sh                      ← master first-time
    ├── daily-restart.sh            ← mỗi lần restart máy
    ├── setup-minikube.sh
    ├── build-images.sh
    ├── install-nginx-ingress.sh
    ├── install-elk.sh
    ├── deploy-stack.sh
    ├── seed-mysql.sh
    └── teardown.sh
```
