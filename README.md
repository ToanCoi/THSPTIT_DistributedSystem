# THSPTIT_DistributedSystem

Hệ thống bán hàng B2C theo kiến trúc microservice — đồ án Hệ thống phân tán, Học kỳ 2, PTIT.

Hệ thống gồm 4 API + 2 worker + 1 API Gateway + 1 SPA, giao tiếp qua REST và Kafka, lưu trữ trên MySQL. Sổ tồn kho (ledger) được ghi nhận từ phiếu nhập/xuất kho thông qua event-driven flow.

## Sơ đồ tổng quan

```
                     Browser
                        │
                        ▼
              ┌──────────────────┐
              │   Frontend SPA   │ Vue 3 + Vuetify
              └────────┬─────────┘
                       │ /Auth, /Business, /Order
                       ▼
              ┌──────────────────┐
              │   ApiGateway     │ .NET 10, HttpClient
              └─┬───────┬────────┘
                │       │
       ┌────────▼─┐  ┌──▼──────┐  ┌─────────┐
       │ AuthApi  │  │Business │  │ Order   │
       │ (JWT)    │  │   Api   │  │  Api    │
       └────┬─────┘  └────┬────┘  └────┬────┘
            │             │            │
            ▼             ▼            ▼
       ┌────────┐   ┌─────────────┐  ┌─────────────┐
       │master  │   │ business_db │  │  Kafka      │
       │  _db   │   │ (11 bảng)   │  │  topics:    │
       │ users  │   │             │  │ - order-    │
       └────────┘   └─────────────┘  │   created   │
                                      │ - ledger-   │
                                      │   change    │
                                      └──────┬──────┘
                                             │
                                ┌────────────┴────────────┐
                                ▼                         ▼
                          ┌──────────┐             ┌───────────┐
                          │Voucher   │ order-      │ Ledger    │ ledger-
                          │Worker    │ created     │ Worker    │ change
                          │ Outward  │             │ ledger    │
                          │ Service  │             │ Service   │
                          └──────────┘             └───────────┘
```

## Tech stack

| Tầng | Công nghệ |
|---|---|
| Backend | .NET 10, ASP.NET Core, Clean Architecture |
| ORM / data access | Dapper + MySQL Connector |
| Database | MySQL 8.0 (2 instance: master_db + business_db) |
| Message queue | Apache Kafka (Confluent.Kafka) |
| API Gateway | ASP.NET Core + HttpClient (custom ProxyService) |
| Frontend | Vue 3.4, Vuetify 3.5, Pinia 2.1, Vue Router 4, Vite 5, Axios |
| Auth | JWT HS256 + BCrypt.Net |
| Logging | NLog (file + console) |
| Container | Docker, docker-compose (cho dev local) |
| Orchestration | Kubernetes local trên minikube + Helm 3 (umbrella chart) |
| Logging stack | ELK (Elasticsearch + Kibana + Logstash + Filebeat) |

## Tài liệu

Đọc tuần tự từ tổng quan đến chi tiết:

| # | Thư mục | Nội dung |
|---|---|---|
| 1 | [docs/01-business/](docs/01-business/README.md) | **Nghiệp vụ** — bài toán, use cases, workflow (đơn → phiếu xuất → sổ cái), data model |
| 2 | [docs/02-architecture/](docs/02-architecture/README.md) | **Kiến trúc** — sơ đồ hệ thống, catalog services, giao tiếp REST + Kafka, cấu trúc code backend/frontend |
| 3 | [docs/03-deployment/](docs/03-deployment/README.md) | **Triển khai** — yêu cầu phần cứng/công cụ, build & run local, deploy k8s local trên minikube |

>  Xem [docs/02-architecture/diagrams.md](docs/02-architecture/diagrams.md) — Mermaid: component overview, Kafka routing, state machine Order/Voucher, k8s deployment topology.

## Quick start

### Phát triển local (nhanh nhất)

```bash
# 1. Khởi động backend (MySQL + Kafka + 4 API + 2 worker) bằng Docker Compose
cd backend
docker compose up -d

# 2. Cài dependencies + chạy frontend (terminal khác)
cd ../frontend
npm install
npm run dev
# Mở http://localhost:3000
```

Đăng nhập mặc định (đã có sẵn trong init.sql):
- Username: `admin`
- Password: `admin123`

Chi tiết: [docs/03-deployment/local-dev.md](docs/03-deployment/local-dev.md)

### Triển khai Kubernetes local trên minikube (môi trường đầy đủ)

```bash
# Yêu cầu: Docker Desktop + minikube + helm + kubectl + jq
./infra/scripts/all.sh

# Sau khi script xong, mở terminal MỚI:
minikube -p ecom tunnel
# (giữ nguyên terminal này)

# Thêm vào hosts (PowerShell Admin, 1 lần):
Add-Content C:\Windows\System32\drivers\etc\hosts "127.0.0.1 ecom.local"
ipconfig /flushdns

# Mở browser: http://ecom.local/
```

Chi tiết: [docs/03-deployment/k8s-deploy.md](docs/03-deployment/k8s-deploy.md)

## Cấu trúc repo

```
THSPTIT_DistributedSystem/
├── README.md                          ← file này
├── docs/                              ← tài liệu (đọc tuần tự 01 → 02 → 03)
│   ├── 01-business/
│   ├── 02-architecture/
│   └── 03-deployment/
├── backend/                           ← .NET solution (Ecom.slnx)
│   ├── ApiGateway/                    ← port 5000, reverse proxy
│   ├── AuthApi/                       ← port 5001, JWT
│   ├── BusinessApi/                   ← port 5002, CRUD nghiệp vụ
│   ├── OrderApi/                      ← port 5003, đơn hàng
│   ├── Workers/
│   │   ├── LedgerWorker/              ← ghi sổ cái
│   │   ├── VoucherWorker/             ← sinh phiếu xuất từ đơn
│   │   └── Workers.Shared/            ← Kafka helpers dùng chung
│   ├── BE.Domain/                     ← entities + repo interfaces
│   ├── BE.Domain.Mysql/               ← repo implementations
│   ├── BE.Application/                ← service implementations
│   ├── BE.Application.Contracts/      ← service interfaces + DTOs
│   ├── BE.HostBase/                   ← DI extensions
│   ├── BE.Domain.Share/               ← models share
│   ├── Scripts/                       ← init.sql, migrations
│   └── docker-compose.yml             ← chạy local
├── frontend/                          ← Vue 3 SPA
│   ├── src/
│   │   ├── views/                     ← 8 màn hình
│   │   ├── stores/                    ← 7 Pinia store
│   │   ├── api/                       ← axios client
│   │   └── router/                    ← Vue Router
│   └── Dockerfile
└── infra/                             ← Helm charts + scripts cho k8s
    ├── charts/                        ← 9 chart (1 umbrella + 7 subchart local + deps)
    ├── scripts/                       ← 9 script bash
    ├── k8s/                           ← raw manifest (kafka.yaml)
    └── logging/                       ← values cho ELK
```
