# 02 — Kiến trúc

Phần này mô tả **kiến trúc kỹ thuật** của hệ thống, đọc từ tổng quan đến chi tiết.

| File | Nội dung |
|---|---|
| [system-context.md](system-context.md) | Sơ đồ C4 (Context → Container → Deployment) |
| [service-catalog.md](service-catalog.md) | Catalog 7 thành phần chính: port, tech, trách nhiệm, file ref |
| [communication.md](communication.md) | Giao tiếp đồng bộ (REST) và bất đồng bộ (Kafka), message schema |
| [data-architecture.md](data-architecture.md) | Kiến trúc dữ liệu: MySQL, Dapper, Repository pattern |
| [backend-codebase.md](backend-codebase.md) | Cấu trúc code backend: Clean Architecture layers, naming, patterns |
| [frontend.md](frontend.md) | Kiến trúc frontend: Vue 3 + Vuetify + Pinia, routing, API client |
| [diagrams.md](diagrams.md) | **Mermaid diagrams** bổ sung: component overview, Kafka routing, state machines Order/Voucher, k8s topology |

## Nguyên tắc thiết kế

### 1. Microservice với event-driven

Hệ thống gồm **4 API service** + **2 worker** + **1 API Gateway** + **1 SPA**. Các service **không gọi nhau qua HTTP** (trừ qua Gateway) — chúng giao tiếp bất đồng bộ qua **Kafka**.

Lý do: tránh coupling chặt, đảm bảo luồng ledger chạy đúng cả khi 1 service tạm thời down.

### 2. Database per service

Mỗi service có database riêng (về mặt logic):
- `AuthApi` → `master_db` (1 bảng `users`)
- `BusinessApi`, `OrderApi`, `LedgerWorker`, `VoucherWorker` → `business_db` (10 bảng)

Hiện tại `business_db` được share vật lý giữa 4 thành phần (vì chưa tách multi-tenant). Nếu scale, có thể tách `business_db` thành nhiều instance.

### 3. API Gateway làm single entry point

Mọi request từ frontend đều qua `ApiGateway` (port 5000). Gateway dùng `HttpClient + custom ProxyService` để forward request — **không dùng YARP** như thiết kế ban đầu (xem chi tiết trong [service-catalog.md](service-catalog.md#apigateway)).

### 4. Worker là background service

Hai worker (`LedgerWorker`, `VoucherWorker`) là `BackgroundService` (IHostedService), **không có HTTP endpoint**, chỉ subscribe Kafka. Tách biệt khỏi API service để scale độc lập và dễ debug.

### 5. Trách nhiệm đã phân chia rõ

| Thành phần | Trách nhiệm chính |
|---|---|
| `ApiGateway` | Forward request, không xử lý nghiệp vụ |
| `AuthApi` | JWT, user management |
| `BusinessApi` | CRUD Customer/Product/Stock/Inward/Outward/ProductPrices |
| `OrderApi` | CRUD Order, publish `order-created`, cascade-delete publish `ledger-change` |
| `VoucherWorker` | Consume `order-created`, sinh Outward (qua OutwardService) |
| `LedgerWorker` | Consume `ledger-change`, ghi ledger |
| `Frontend` | UI người dùng |

## Tech stack tổng quan

| Tầng | Công nghệ |
|---|---|
| Backend framework | .NET 10, ASP.NET Core, C# 13 |
| Kiến trúc code | Clean Architecture (BE.Domain, BE.Application, BE.Application.Contracts) |
| ORM / data access | Dapper + MySqlConnector |
| Database | MySQL 8.0 (2 instance: master_db + business_db) |
| Message queue | Apache Kafka 3.8.1 (KRaft mode), Confluent.Kafka client |
| Auth | JWT HS256 + BCrypt.Net |
| API Gateway | ASP.NET Core HttpClient + custom ProxyService |
| Logging | NLog (file + console) |
| API doc | Swagger/OpenAPI (mỗi service có `/swagger`) |
| Frontend framework | Vue 3.4 |
| UI library | Vuetify 3.5 |
| State management | Pinia 2.1 |
| Routing | Vue Router 4 |
| HTTP client | Axios 1.6 |
| Build tool | Vite 5 |
| Testing | Vitest |
| Container | Docker (cho mỗi service) |
| Orchestration | Kubernetes local (minikube), Helm 3 (umbrella chart) |
| Logging stack | ELK (Elasticsearch + Kibana + Logstash + Filebeat) |