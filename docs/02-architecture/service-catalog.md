# Catalog Services

Tài liệu này liệt kê **7 thành phần runtime** chính của hệ thống, kèm port, công nghệ, trách nhiệm và file tham chiếu.

## Tổng quan

| # | Thành phần | Type | Port (docker-compose) | Port (k8s) | Tech chính |
|---|---|---|---|---|---|
| 1 | ApiGateway | ASP.NET Core | 5000 | 80 | .NET 10, HttpClient |
| 2 | AuthApi | ASP.NET Core | 5001 | 80 | .NET 10, JWT HS256, BCrypt |
| 3 | BusinessApi | ASP.NET Core | 5002 | 80 | .NET 10, Dapper, Kafka producer |
| 4 | OrderApi | ASP.NET Core | 5003 | 80 | .NET 10, Dapper, Kafka producer |
| 5 | VoucherWorker | BackgroundService | - | - | .NET Hosted, Kafka consumer |
| 6 | LedgerWorker | BackgroundService | - | - | .NET Hosted, Kafka consumer |
| 7 | Frontend (SPA) | nginx serving Vue | 3000 (Vite dev) / 80 (Docker) | 80 | Vue 3, Vuetify, Pinia |

Ngoài ra còn 2 MySQL instance và 1 Kafka broker — xem [system-context.md](system-context.md).

---

## 1. ApiGateway

| Thuộc tính | Giá trị |
|---|---|
| **Type** | ASP.NET Core Web Application |
| **Port** | 5000 (docker-compose) / 80 (k8s) |
| **Trách nhiệm** | Forward HTTP request từ frontend → 1 trong 3 API service |
| **Tech** | .NET 10, ASP.NET Core, HttpClient, custom ProxyService |
| **Database** | Không có |
| **Khởi động** | `dotnet run --project backend/ApiGateway` hoặc container `api-gateway` |
| **Entry point** | `backend/ApiGateway/Program.cs` |
| **Routes** | `/Auth/*`, `/Business/*`, `/Order/*` |
| **Health check** | `GET /health` → `{ status: "ok", service: "ApiGateway" }` |

### Lưu ý quan trọng

- ApiGateway dùng **HttpClient + custom ProxyService** (`backend/ApiGateway/Program.cs:117-193`), **KHÔNG dùng YARP** như thiết kế ban đầu.
- Khi dev với `dotnet run` (không qua docker), default env là:
  - `Services__AuthApi = http://localhost:5289`
  - `Services__BusinessApi = http://localhost:5119`
  - `Services__OrderApi = http://localhost:5120`
- Khi chạy qua docker-compose (`backend/docker-compose.yml`), các URL được override thành `http://auth-api:5001`, etc.
- Route `/Business/*` và `/Order/*` rewrite thành `/{path}` vì BusinessApi và OrderApi mount ở `/api/...`. `/Auth/*` giữ nguyên vì AuthApi mount ở root (`[Route("[controller]")]`).

### Ví dụ flow

```
GET /Auth/login           → forward GET /Auth/login           → AuthApi
GET /Business/api/customers → forward GET /api/customers        → BusinessApi
GET /Order/api/orders     → forward GET /api/orders            → OrderApi
```

---

## 2. AuthApi

| Thuộc tính | Giá trị |
|---|---|
| **Type** | ASP.NET Core Web API |
| **Port** | 5001 (docker-compose) / 5289 (default `dotnet run`) / 80 (k8s) |
| **Trách nhiệm** | Authentication: login, register, Google login, refresh token, JWT |
| **Tech** | .NET 10, ASP.NET Core, JWT Bearer, BCrypt.Net |
| **Database** | `master_db` (1 bảng `users`) |
| **Entry point** | `backend/AuthApi/Program.cs` |
| **Controller** | `AuthController` (`backend/AuthApi/Controllers/AuthController.cs`) |
| **Route prefix** | `[Route("[controller]")]` → `/Auth/...` |

### Endpoints

| Method | Path | Auth | Mô tả |
|---|---|---|---|
| POST | `/Auth/login` | Anonymous | Login bằng username/password → JWT + refresh_token |
| POST | `/Auth/register` | Anonymous | Đăng ký user mới |
| POST | `/Auth/google` | Anonymous | Login qua Google OAuth |
| POST | `/Auth/refresh-token` | Anonymous | Lấy access token mới từ refresh_token |
| POST | `/Auth/logout` | Authorize | Revoke refresh token hiện tại |
| POST | `/Auth/change-password` | Authorize | Đổi mật khẩu |
| GET | `/Auth/me` | Authorize | Thông tin user hiện tại |

### JWT config

- Algorithm: HS256
- Secret: từ `Jwt:SecretKey` (default `Ecom_Microservice_SecretKey_MustBeAtLeast32Characters!2026`)
- Issuer: `Ecom.AuthApi`
- Audience: `Ecom.Client`
- Expiration: 24h (access token)
- Refresh token: lưu trong `users.refresh_token` + expire

---

## 3. BusinessApi

| Thuộc tính | Giá trị |
|---|---|
| **Type** | ASP.NET Core Web API |
| **Port** | 5002 (docker-compose) / 5119 (default `dotnet run`) / 80 (k8s) |
| **Trách nhiệm** | CRUD Customer/Product/Stock/Inward/Outward/ProductPrices + publish Kafka |
| **Tech** | .NET 10, ASP.NET Core, Dapper, Kafka producer |
| **Database** | `business_db` (10 bảng) |
| **Entry point** | `backend/BusinessApi/Program.cs` |
| **Route prefix** | `[Route("api/[controller]")]` → `/api/...` |

### Controllers (6)

| Controller | Route | File |
|---|---|---|
| `CustomersController` | `/api/customers` | `backend/BusinessApi/Controllers/CustomersController.cs` |
| `ProductsController` | `/api/products` | `backend/BusinessApi/Controllers/ProductsController.cs` |
| `StocksController` | `/api/stocks` | `backend/BusinessApi/Controllers/StocksController.cs` |
| `InwardsController` | `/api/inwards` | `backend/BusinessApi/Controllers/InwardsController.cs` |
| `OutwardsController` | `/api/outwards` | `backend/BusinessApi/Controllers/OutwardsController.cs` |
| `ProductPricesController` | `/api/productprices` | `backend/BusinessApi/Controllers/ProductPricesController.cs` |

Mỗi controller có 7 endpoints chuẩn: `GET`, `GET /paging`, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}`. Tổng cộng ~42 endpoints.

### Kafka producer

- Inject `IKafkaProducerService` (singleton) — `backend/BusinessApi/Program.cs:100-102`
- Service dùng khi tạo/sửa/xóa Inward, Outward: publish `ledger-change`
- Inward publish sync (chờ ack), Outward fire-and-forget

---

## 4. OrderApi

| Thuộc tính | Giá trị |
|---|---|
| **Type** | ASP.NET Core Web API |
| **Port** | 5003 (docker-compose) / 5120 (default `dotnet run`) / 80 (k8s) |
| **Trách nhiệm** | CRUD Order + publish `order-created` + cascade-delete publish `ledger-change` |
| **Tech** | .NET 10, ASP.NET Core, Dapper, Kafka producer |
| **Database** | `business_db` (4 bảng: orders, order_items, outwards) |
| **Entry point** | `backend/OrderApi/Program.cs` |
| **Route prefix** | `[Route("api/[controller]")]` → `/api/...` |
| **Controller** | `OrdersController` (`backend/OrderApi/Controllers/OrdersController.cs`) |

### Endpoints

| Method | Path | Auth | Mô tả |
|---|---|---|---|
| GET | `/api/orders` | Authorize | Tất cả orders |
| GET | `/api/orders/paging` | Authorize | Phân trang |
| GET | `/api/orders/{id}` | Authorize | Chi tiết 1 order |
| POST | `/api/orders` | Authorize | Tạo order → publish `order-created` |
| PUT | `/api/orders/{id}` | Authorize | Cập nhật order (tạo mới items, xóa cũ) |
| PUT | `/api/orders/{id}/status` | Authorize | Cập nhật status |
| DELETE | `/api/orders/{id}` | Authorize | Xóa order (cascade) → publish `ledger-change UPDATE qty=0` |

### Kafka producer (2 loại)

- `IKafkaProducerService` (singleton) — dùng cho `ledger-change` khi cascade delete
- `IProducer<string, string>` (khởi tạo trực tiếp trong `OrderService`) — dùng cho `order-created` khi tạo đơn

Lý do 2 loại: thiết kế cũ dùng trực tiếp `Confluent.Kafka.IProducer`, sau đó refactor `IKafkaProducerService` cho Inward/Outward. OrderService chưa refactor.

### Order code

- Format: `DH{sequence}` (vd: `DH1`, `DH2`, ...)
- Tự sinh qua `OrderRepo.GetNextOrderCodeAsync()` với row lock trong transaction

---

## 5. VoucherWorker

| Thuộc tính | Giá trị |
|---|---|
| **Type** | .NET BackgroundService (Hosted Service) |
| **Port** | Không có (không có HTTP) |
| **Trách nhiệm** | Consume `order-created` → tạo outward cho mỗi item |
| **Tech** | .NET 10 Hosted Service, Confluent.Kafka consumer |
| **Database** | `business_db` (gọi `OutwardService.CreateAsync` → INSERT outwards) |
| **Entry point** | `backend/Workers/VoucherWorker/Program.cs` |
| **Consumer class** | `VoucherKafkaConsumer` (line 70-164) |

### Flow xử lý message

1. Subscribe `order-created` (group `voucher-worker-group`)
2. Nhận message JSON → deserialize `OrderCreatedMessage`
3. Với mỗi item trong order → tạo `OutwardCreateDto` → gọi `OutwardService.CreateAsync()`
4. `OutwardService` tự publish `ledger-change` (không publish ở worker — quan trọng!)

### Lưu ý

- Worker KHÔNG publish Kafka riêng — chỉ gọi `OutwardService`. Comment trong code: `Program.cs:152-153` cảnh báo nếu publish ở worker sẽ bị ghi ledger 2 lần.
- `IKafkaProducerService` được đăng ký trong DI nhưng thực tế không dùng trực tiếp trong worker (chỉ OutwardService dùng). Có thể bỏ trong tương lai.

---

## 6. LedgerWorker

| Thuộc tính | Giá trị |
|---|---|
| **Type** | .NET BackgroundService (Hosted Service) |
| **Port** | Không có |
| **Trách nhiệm** | Consume `ledger-change` → ghi sổ cái |
| **Tech** | .NET 10 Hosted Service, Confluent.Kafka consumer |
| **Database** | `business_db` (3 bảng: ledger, ledger_date, ledger_closing) |
| **Entry point** | `backend/Workers/LedgerWorker/Program.cs` |
| **Consumer class** | `LedgerKafkaConsumer` (line 66-148) |
| **Service xử lý** | `LedgerService` (`backend/Workers/LedgerWorker/LedgerService.cs`) |

### Flow xử lý message

1. Subscribe `ledger-change` (group `ledger-worker-group`)
2. Deserialize `LedgerChangeMessage`
3. Phân loại theo `event_type` (default `CREATE`):
   - `CREATE` + `voucher_type=INWARD` → `ProcessInwardAsync`
   - `CREATE` + `voucher_type=OUTWARD` → `ProcessOutwardAsync`
   - `UPDATE` → `ProcessUpdateAsync` (reverse + rebuild)

### 3 method xử lý chính

| Method | Line | Xử lý |
|---|---|---|
| `ProcessInwardAsync` | 27-53 | INSERT ledger + UpsertLedgerDate(+qty) + UpsertClosing(+qty) |
| `ProcessOutwardAsync` | 56-82 | INSERT ledger + UpsertLedgerDate(-qty) + UpsertClosing(-qty) |
| `ProcessUpdateAsync` | 108-208 | Reverse impact cũ + xóa entries cũ + insert mới + apply impact mới |

---

## 7. Frontend (SPA)

| Thuộc tính | Giá trị |
|---|---|
| **Type** | Vue 3 SPA + nginx serving |
| **Port** | 3000 (Vite dev) / 80 (Docker, k8s) |
| **Trách nhiệm** | UI người dùng |
| **Tech** | Vue 3.4, Vuetify 3.5, Pinia 2.1, Vue Router 4, Vite 5, Axios 1.6 |
| **Backend** | Gọi qua API Gateway (không gọi thẳng API service) |
| **Entry point** | `frontend/src/main.js`, `frontend/src/App.vue` |
| **Dockerfile** | `frontend/Dockerfile` (multi-stage: node:20-alpine → nginx:1.27-alpine) |

### Cấu trúc thư mục

```
frontend/
├── src/
│   ├── main.js                # Khởi tạo Pinia + Vuetify + Router
│   ├── App.vue                # Layout: v-app-bar + v-navigation-drawer
│   ├── api/
│   │   └── client.js          # Axios instance + 8 nhóm endpoint
│   ├── stores/                # 7 Pinia store (auth, customer, product, stock, inward, outward, order)
│   ├── views/                 # 8 view component
│   │   ├── Auth/              # LoginView, RegisterView
│   │   ├── Customer/CustomerView.vue
│   │   ├── Product/ProductView.vue
│   │   ├── Stock/StockView.vue
│   │   ├── Inward/InwardView.vue
│   │   ├── Outward/OutwardView.vue
│   │   └── Order/OrderView.vue
│   ├── router/
│   │   └── index.js           # 8 route + requiresAuth guard
│   ├── utils/
│   │   └── date.js            # Format date helper
│   └── assets/
├── public/
├── vite.config.js             # Alias @, proxy /Auth /Business /Order → localhost:62739
├── package.json
├── Dockerfile
├── nginx.conf                 # SPA fallback: try_files $uri /index.html
└── .dockerignore
```

### Vite proxy (dev)

```js
// vite.config.js
server: {
  port: 3000,
  proxy: {
    '/Auth': 'http://localhost:62739',
    '/Business': 'http://localhost:62739',
    '/Order': 'http://localhost:62739'
  }
}
```

Lưu ý: port `62739` là default port khi `dotnet run` 1 ASP.NET project không set `ASPNETCORE_URLS`. Nếu muốn dev với ApiGateway (port 5000), sửa proxy thành `http://localhost:5000`.

Chi tiết kiến trúc frontend: xem [frontend.md](frontend.md).