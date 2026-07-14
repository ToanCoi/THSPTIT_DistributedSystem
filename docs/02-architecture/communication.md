# Giao tiếp giữa các thành phần

Hệ thống dùng **2 cơ chế giao tiếp**: REST (đồng bộ) qua API Gateway, và Kafka (bất đồng bộ) cho event-driven flow.

## 1. Giao tiếp đồng bộ — REST API

### 1.1 Pattern

```
Browser → Frontend SPA → ApiGateway (port 5000) → Backend API service
                                              ├─ AuthApi:5001
                                              ├─ BusinessApi:5002
                                              └─ OrderApi:5003
```

- Frontend **KHÔNG gọi thẳng** backend service, luôn qua Gateway
- Backend service **KHÔNG gọi nhau qua HTTP** (trừ `OutwardService.CreateAsync` được VoucherWorker gọi qua DI trong cùng process — không phải HTTP)
- Mọi request đều cần JWT (trừ `/Auth/login`, `/Auth/register`, `/Auth/google`, `/Auth/refresh-token`)

### 1.2 URL convention

| Từ client | Qua Gateway | Forward đến | URL thật |
|---|---|---|---|
| `GET /Auth/me` | `/Auth/*` | AuthApi | `http://auth-api:5001/Auth/me` |
| `POST /Business/api/customers` | `/Business/*` | BusinessApi | `http://business-api:5002/api/customers` |
| `GET /Order/api/orders/123` | `/Order/*` | OrderApi | `http://order-api:5003/api/orders/123` |

Lưu ý:
- Gateway rewrite `/Business/{path}` → `{path}` (vì BusinessApi mount ở `/api/...`)
- Gateway rewrite `/Order/{path}` → `{path}`
- Gateway giữ nguyên `/Auth/{path}` (vì AuthApi mount ở root)

### 1.3 Authentication

Header mọi request (trừ anonymous):
```
Authorization: Bearer <jwt_access_token>
```

Token lấy từ `POST /Auth/login`, lưu `localStorage.token` ở frontend. Axios interceptor tự gắn (xem [frontend.md](frontend.md)).

JWT validate ở mọi API service với cùng secret/issuer/audience (`backend/{AuthApi,BusinessApi,OrderApi}/Program.cs`).

### 1.4 Bảng endpoints đầy đủ

**AuthApi** (7 endpoints):

| Method | Path | Auth | Mô tả |
|---|---|---|---|
| POST | `/Auth/login` | Anonymous | Login |
| POST | `/Auth/register` | Anonymous | Đăng ký |
| POST | `/Auth/google` | Anonymous | Google login |
| POST | `/Auth/refresh-token` | Anonymous | Refresh |
| POST | `/Auth/logout` | Required | Logout |
| POST | `/Auth/change-password` | Required | Đổi pass |
| GET | `/Auth/me` | Required | User hiện tại |

**BusinessApi** (~42 endpoints, 7 mỗi controller × 6 controller):

| Method | Pattern (áp dụng cho customers, products, stocks, inwards, outwards, productprices) |
|---|---|
| GET | `/api/{entity}` — lấy tất cả |
| GET | `/api/{entity}/paging?skip=&take=&sort_field=&sort_order=` — phân trang |
| GET | `/api/{entity}/{id}` — chi tiết |
| POST | `/api/{entity}` — tạo mới |
| PUT | `/api/{entity}/{id}` — cập nhật |
| DELETE | `/api/{entity}/{id}` — xóa |

`ProductPricesController` có thêm 2 endpoints đặc biệt:
- `GET /api/productprices/{productId}/selling-price` — giá bán
- `GET /api/productprices/{productId}/stock/{stockId}` — tồn kho

**OrderApi** (7 endpoints):

| Method | Path | Mô tả |
|---|---|---|
| GET | `/api/orders` | Tất cả |
| GET | `/api/orders/paging` | Phân trang |
| GET | `/api/orders/{id}` | Chi tiết |
| POST | `/api/orders` | Tạo (publish `order-created`) |
| PUT | `/api/orders/{id}` | Cập nhật |
| PUT | `/api/orders/{id}/status` | Cập nhật status |
| DELETE | `/api/orders/{id}` | Xóa (publish `ledger-change UPDATE qty=0`) |

### 1.5 Versioning

**Không có versioning**. Tất cả endpoints ở root path (không có `/v1/`, `/v2/`). Nếu cần thay đổi breaking → tạo endpoint mới và giữ cũ.

---

## 2. Giao tiếp bất đồng bộ — Kafka

### 2.1 Topic tổng quan

| Topic | Producer | Consumer | Trigger | Format |
|---|---|---|---|---|
| `order-created` | `OrderApi.OrderService` | `VoucherWorker` | Tạo đơn hàng | JSON `OrderCreatedMessage` |
| `ledger-change` | `BusinessApi.OutwardService`, `BusinessApi.InwardService`, `OrderApi.OrderService` | `LedgerWorker` | Tạo/sửa/xóa phiếu nhập/xuất, cascade delete đơn | JSON `LedgerChangeMessage` |

### 2.2 `order-created` message

File model: `backend/Workers/Workers.Shared/Models/OrderCreatedMessage.cs`

```csharp
public class OrderCreatedMessage
{
    public string order_id { get; set; }
    public string customer_id { get; set; }
    public string stock_id { get; set; }       // Thêm mới so với PRD
    public string order_code { get; set; }
    public List<OrderItemMessage> items { get; set; }
    public string timestamp { get; set; }      // ISO 8601
}

public class OrderItemMessage
{
    public string product_id { get; set; }
    public decimal quantity { get; set; }
    public decimal unit_price { get; set; }
}
```

JSON mẫu:
```json
{
  "order_id": "abc-123",
  "customer_id": "xyz-789",
  "stock_id": "stock-456",
  "order_code": "DH7",
  "items": [
    { "product_id": "p-001", "quantity": 5, "unit_price": 100000 }
  ],
  "timestamp": "2026-07-14T10:30:00.000Z"
}
```

Producer code: `OrderService.PublishOrderCreatedMessage` (`backend/BE.Application/Services/Order/OrderService.cs:245-271`).

### 2.3 `ledger-change` message

File model: `backend/Workers/Workers.Shared/Models/LedgerChangeMessage.cs`

```csharp
public class LedgerChangeMessage
{
    public string voucher_id { get; set; }
    public string voucher_type { get; set; }       // "INWARD" hoặc "OUTWARD"
    public string product_id { get; set; }
    public string stock_id { get; set; }
    public decimal quantity { get; set; }
    public string timestamp { get; set; }

    // Mặc định "CREATE", "UPDATE" khi sửa phiếu hoặc xóa đơn cascade
    public string event_type { get; set; } = "CREATE";

    // Chỉ dùng khi event_type = "UPDATE"
    public decimal? old_quantity { get; set; }
    public string? old_product_id { get; set; }
    public string? old_stock_id { get; set; }
}
```

JSON mẫu CREATE (tạo phiếu nhập):
```json
{
  "voucher_id": "inw-001",
  "voucher_type": "INWARD",
  "product_id": "p-001",
  "stock_id": "s-001",
  "quantity": 50,
  "timestamp": "2026-07-14T10:30:00.000Z",
  "event_type": "CREATE"
}
```

JSON mẫu UPDATE (sửa phiếu nhập, đổi số lượng 50→30):
```json
{
  "voucher_id": "inw-001",
  "voucher_type": "INWARD",
  "product_id": "p-001",
  "stock_id": "s-001",
  "quantity": 30,
  "timestamp": "2026-07-14T11:00:00.000Z",
  "event_type": "UPDATE",
  "old_quantity": 50,
  "old_product_id": "p-001",
  "old_stock_id": "s-001"
}
```

JSON mẫu UPDATE qty=0 (xóa đơn cascade):
```json
{
  "voucher_id": "out-001",
  "voucher_type": "OUTWARD",
  "product_id": "p-001",
  "stock_id": "s-001",
  "quantity": 0,
  "timestamp": "2026-07-14T12:00:00.000Z",
  "event_type": "UPDATE",
  "old_quantity": 5,
  "old_product_id": "p-001",
  "old_stock_id": "s-001"
}
```

### 2.4 Producer

| Class | File | Dùng ở đâu |
|---|---|---|
| `IKafkaProducerService` (Singleton) | `backend/Workers/Workers.Shared/Services/KafkaProducerService.cs` | InwardService, OutwardService, OrderService (cascade delete) |
| `IProducer<string, string>` (trực tiếp) | (Confluent.Kafka) | OrderService.CreateAsync — tạo đơn (line 29, 56-63) |

Kafka producer config:
- BootstrapServers: `Kafka:BootstrapServers` (default `localhost:9093`)
- Acks: `Acks.All` (chờ tất cả replica ack)
- SocketTimeoutMs: 5000
- MessageTimeoutMs: 5000

### 2.5 Consumer

| Worker | Subscribe | Group ID | Class |
|---|---|---|---|
| `VoucherWorker` | `order-created` | `voucher-worker-group` | `VoucherKafkaConsumer` (`backend/Workers/VoucherWorker/Program.cs:70`) |
| `LedgerWorker` | `ledger-change` | `ledger-worker-group` | `LedgerKafkaConsumer` (`backend/Workers/LedgerWorker/Program.cs:66`) |

Config:
- AutoOffsetReset: `Earliest` (đọc từ đầu nếu group mới)
- EnableAutoCommit: `true`

### 2.6 Topic auto-create

Trong docker-compose, `KAFKA_AUTO_CREATE_TOPICS_ENABLE=true` → topic tự tạo khi có message đầu tiên.

Trong k8s, file `infra/k8s/kafka.yaml` không set auto-create. Nếu deploy mà không có message nào trước, topic sẽ không tồn tại → consumer sẽ block. Để tạo topic thủ công:

```bash
MSYS_NO_PATHCONV=1 kubectl -n ecom exec kafka-0 -- \
  /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 \
  --create --topic order-created --partitions 1 --replication-factor 1

MSYS_NO_PATHCONV=1 kubectl -n ecom exec kafka-0 -- \
  /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 \
  --create --topic ledger-change --partitions 1 --replication-factor 1
```

Hoặc đơn giản hơn: gửi 1 request tạo đơn qua API → topic tự tạo (nếu auto-create enabled trong kafka.yaml — kiểm tra).

### 2.7 Debug Kafka

Xem message trong topic:
```bash
MSYS_NO_PATHCONV=1 kubectl -n ecom exec kafka-0 -- \
  /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 \
  --topic ledger-change \
  --from-beginning
```

Xem worker log:
```bash
kubectl -n ecom logs -l app.kubernetes.io/name=voucher-worker --tail=50 -f
kubectl -n ecom logs -l app.kubernetes.io/name=ledger-worker --tail=50 -f
```

Local (docker compose):
```bash
docker logs ecom-voucher-worker -f
docker logs ecom-ledger-worker -f
```

---

## 3. Không có

- ❌ **gRPC** — không dùng
- ❌ **Redis** — không có cache, không có session store
- ❌ **RabbitMQ / ActiveMQ** — chỉ Kafka
- ❌ **Service mesh (Istio, Linkerd)** — không có
- ❌ **Circuit breaker / retry policy** — không có (cần thêm nếu scale)
- ❌ **Distributed tracing (Jaeger, Zipkin)** — không có
- ❌ **API rate limiting** — không có
- ❌ **Webhook ra ngoài** — không có

Mọi event-driven flow đều dựa vào **at-least-once delivery** của Kafka + idempotent consumer (LedgerWorker xử lý trùng message an toàn vì dùng `reference_id` lookup).