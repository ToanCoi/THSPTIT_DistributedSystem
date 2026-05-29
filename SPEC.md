# SPEC: Hệ thống Phân tán - Microservice Application

## 1. Tổng quan

### 1.1 Mục tiêu
Xây dựng ứng dụng bán hàng trực tuyến (B2C) theo kiến trúc microservice với:
- **Backend**: .NET 10 Clean Architecture
- **Frontend**: Vue3 (repo riêng)
- **Giao tiếp đồng bộ**: RESTful API qua API Gateway
- **Giao tiếp bất đồng bộ**: Kafka Message Queue

### 1.2 Phạm vi Demo (2 tuần)
- 1 master database (auth)
- 1 business database (shared cho demo, kiến trúc multi-tenant để sau)
- 4 backend services + 1 API Gateway
- 1 Kafka consumer (HandleWorker)

---

## 2. Kiến trúc hệ thống

### 2.1 Sơ đồ services

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend (Vue3)                        │
│                    (gọi qua ApiGateway port 5000)               │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      ApiGateway (YARP)                          │
│                   (port 5000 - Reverse Proxy)                    │
│         /Auth/* → AuthApi:5001                                  │
│         /Business/* → BusinessApi:5002                          │
│         /Order/* → OrderApi:5003                                │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────┬─────────────┬─────────────┬─────────────┐
│  AuthApi    │ BusinessApi │  OrderApi   │HandleWorker │
│  (port5001) │ (port 5002) │ (port 5003) │ (background)│
│   JWT Auth  │CRUD Business│   Orders   │Kafka Consumer│
└─────┬───────┴──────┬──────┴──────┬──────┴──────┬──────┘
      ↓              ↓             ↓             ↓
┌──────────┐  ┌──────────────────────────────┐
│master_db │  │        business_db          │
│ (auth)   │  │  (customers, products,      │
│  users   │  │   stocks, inwards,          │
└──────────┘  │   outwards, orders,         │
              │   ledgers)                   │
              └──────────────────────────────┘
                              ↑
                    HandleWorker ghi ledger
                              ↑
              Kafka topic: "order-created"
```

### 2.2 Cấu trúc thư mục

```
THSPTIT_DistributedSystem/
├── backend/
│   ├── AuthApi/              # Authentication Service
│   ├── BusinessApi/          # Business Logic (Customer, Product, Stock, Inward, Outward)
│   ├── OrderApi/             # Order Management
│   ├── HandleWorker/         # Kafka Consumer → Ledger
│   ├── ApiGateway/           # YARP Reverse Proxy
│   └── Shared/               # BE.Application, BE.Domain, BE.Application.Contracts
├── frontend/                 # Vue3 (repo riêng)
└── docs/                     # Báo cáo
```

---

## 3. Chi tiết Services

### 3.1 AuthApi (port 5001)
**Database**: master_db
**Chức năng**: Authentication với JWT

| Endpoint | Method | Mô tả |
|----------|--------|-------|
| `/Auth/login` | POST | Đăng nhập user/pass → JWT |
| `/Auth/register` | POST | Đăng ký tài khoản mới |
| `/Auth/google` | POST | Đăng nhập Google → JWT |

**Tables**: `users` (user_id, username, password_hash, email, google_id, full_name, role, created_date)

### 3.2 BusinessApi (port 5002)
**Database**: business_db
**Controllers**: 5 controllers

| Controller | Endpoints | Mô tả |
|------------|-----------|--------|
| Customer | GET, POST, PUT, DELETE /api/customers | Quản lý khách hàng |
| Product | GET, POST, PUT, DELETE /api/products | Quản lý hàng hóa |
| Stock | GET /api/stocks | Xem danh sách kho |
| Inward | GET, POST /api/inwards | Nhập kho (phiếu nhập) |
| Outward | GET, POST /api/outwards | Xuất kho (phiếu xuất) |

### 3.3 OrderApi (port 5003)
**Database**: business_db

| Controller | Endpoints | Mô tả |
|------------|-----------|--------|
| Order | GET, POST /api/orders | Tạo đơn hàng, publish Kafka |

**Flow tạo đơn:**
1. `POST /api/orders` → Lưu orders + order_items vào DB
2. Publish message `{ order_id, customer_id, items }` vào Kafka topic `order-created`

### 3.4 HandleWorker (Background Service)
**Database**: business_db (write ledger)
**Chức năng**: Kafka consumer

**Flow xử lý message:**
1. Consume từ topic `order-created`
2. Với mỗi item trong order:
   - Insert vào `led_inventory_item_ledger`
   - Insert/Update vào `led_inventory_item_ledger_date`
   - Update `led_inventory_item_ledger_closing`
3. Logging kết quả

---

## 4. Database Schema

### 4.1 master_db (Auth)
```sql
CREATE TABLE users (
    user_id VARCHAR(36) PRIMARY KEY,
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255),
    email VARCHAR(255),
    google_id VARCHAR(255),
    full_name VARCHAR(255),
    role VARCHAR(50),
    created_date DATETIME
);
```

### 4.2 business_db
```sql
-- Customers
CREATE TABLE customers (
    customer_id VARCHAR(36) PRIMARY KEY,
    user_id VARCHAR(36),
    full_name VARCHAR(255),
    phone VARCHAR(20),
    email VARCHAR(255),
    address TEXT,
    created_date DATETIME
);

-- Products
CREATE TABLE products (
    product_id VARCHAR(36) PRIMARY KEY,
    product_code VARCHAR(50) UNIQUE,
    product_name VARCHAR(255),
    price DECIMAL(18,2),
    unit VARCHAR(50),
    created_date DATETIME
);

-- Stocks (Kho hàng)
CREATE TABLE stocks (
    stock_id VARCHAR(36) PRIMARY KEY,
    stock_code VARCHAR(50) UNIQUE,
    stock_name VARCHAR(255),
    address TEXT,
    created_date DATETIME
);

-- Inwards (Nhập kho)
CREATE TABLE inwards (
    inward_id VARCHAR(36) PRIMARY KEY,
    product_id VARCHAR(36),
    stock_id VARCHAR(36),
    quantity DECIMAL(18,4),
    unit_price DECIMAL(18,2),
    supplier VARCHAR(255),
    invoice_date DATE,
    created_date DATETIME
);

-- Outwards (Xuất kho)
CREATE TABLE outwards (
    outward_id VARCHAR(36) PRIMARY KEY,
    order_id VARCHAR(36),
    product_id VARCHAR(36),
    stock_id VARCHAR(36),
    quantity DECIMAL(18,4),
    unit_price DECIMAL(18,2),
    outward_date DATE,
    created_date DATETIME
);

-- Orders
CREATE TABLE orders (
    order_id VARCHAR(36) PRIMARY KEY,
    customer_id VARCHAR(36),
    order_code VARCHAR(50),
    total_amount DECIMAL(18,2),
    status VARCHAR(50),
    order_date DATE,
    created_date DATETIME
);

-- Order Items
CREATE TABLE order_items (
    order_item_id VARCHAR(36) PRIMARY KEY,
    order_id VARCHAR(36),
    product_id VARCHAR(36),
    quantity DECIMAL(18,4),
    unit_price DECIMAL(18,2),
    created_date DATETIME
);

-- Ledger Tables (Worker write)
CREATE TABLE led_inventory_item_ledger (
    ledger_id VARCHAR(36) PRIMARY KEY,
    product_id VARCHAR(36),
    stock_id VARCHAR(36),
    inward_quantity DECIMAL(18,4),
    outward_quantity DECIMAL(18,4),
    reference_id VARCHAR(36),
    reference_type VARCHAR(50),
    ledger_date DATETIME,
    created_date DATETIME
);

CREATE TABLE led_inventory_item_ledger_date (
    ledger_date_id VARCHAR(36) PRIMARY KEY,
    product_id VARCHAR(36),
    stock_id VARCHAR(36),
    inward_quantity DECIMAL(18,4),
    outward_quantity DECIMAL(18,4),
    ledger_date DATE,
    created_date DATETIME
);

CREATE TABLE led_inventory_item_ledger_closing (
    closing_id VARCHAR(36) PRIMARY KEY,
    product_id VARCHAR(36),
    stock_id VARCHAR(36),
    quantity DECIMAL(18,4),
    updated_date DATETIME
);
```

---

## 5. Message Queue (Kafka)

### 5.1 Topic
- **Topic name**: `order-created`
- **Partitions**: 1 (demo)
- **Replication**: 1

### 5.2 Message Schema
```json
{
  "order_id": "uuid",
  "customer_id": "uuid",
  "order_code": "ORD001",
  "items": [
    {
      "product_id": "uuid",
      "quantity": 10,
      "unit_price": 100000
    }
  ],
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### 5.3 Consumer Flow
```
HandleWorker
  ├── Subscribe: order-created topic
  ├── OnMessage:
  │   ├── Parse order details
  │   ├── For each item:
  │   │   ├── Insert led_inventory_item_ledger
  │   │   ├── Upsert led_inventory_item_ledger_date
  │   │   └── Upsert led_inventory_item_ledger_closing
  │   └── Log success/failure
  └── Auto-commit offset
```

---

## 6. API Gateway Configuration

### 6.1 YARP Routes (programmatic)
```json
{
  "Routes": [
    {
      "RouteId": "auth",
      "ClusterId": "auth-cluster",
      "Match": { "Path": "/Auth/{**catch-all}" },
      "Transforms": [{ "PathPattern": "/{**catch-all}" }]
    },
    {
      "RouteId": "business",
      "ClusterId": "business-cluster",
      "Match": { "Path": "/Business/{**catch-all}" },
      "Transforms": [{ "PathPattern": "/api/{**catch-all}" }]
    },
    {
      "RouteId": "order",
      "ClusterId": "order-cluster",
      "Match": { "Path": "/Order/{**catch-all}" },
      "Transforms": [{ "PathPattern": "/api/{**catch-all}" }]
    }
  ],
  "Clusters": {
    "auth-cluster": { "Destinations": { "auth": { "Address": "http://localhost:5001" } } },
    "business-cluster": { "Destinations": { "business": { "Address": "http://localhost:5002" } } },
    "order-cluster": { "Destinations": { "order": { "Address": "http://localhost:5003" } } }
  }
}
```

### 6.2 URLs mapping
| Client URL | Gateway | Service | Actual URL |
|------------|---------|---------|------------|
| `/Auth/login` | /Auth/* | AuthApi:5001 | `http://localhost:5001/Auth/login` |
| `/Business/customers` | /Business/* | BusinessApi:5002 | `http://localhost:5002/api/customers` |
| `/Order/orders` | /Order/* | OrderApi:5003 | `http://localhost:5003/api/orders` |

---

## 7. Authentication (JWT)

### 7.1 Token Structure
- **Algorithm**: HS256
- **Expiration**: 24 hours
- **Payload**: `{ sub: user_id, username, role, exp, iat }`

### 7.2 Protected Endpoints
Tất cả endpoints trừ `/Auth/login`, `/Auth/register` đều cần JWT Bearer token.

---

## 8. Logging (NLog)

### 8.1 Configuration
- **Output**: File `logs/{shortdate}.log`
- **Log level**: Info (có thể bật Debug cho dev)

### 8.2 Log Format
```
${env} | ${level} | ${logger} | ${message} | ${request-url} | ${thread-id}
```

### 8.3 Log Locations
```
backend/
├── AuthApi/logs/
├── BusinessApi/logs/
├── OrderApi/logs/
├── HandleWorker/logs/
└── ApiGateway/logs/
```

---

## 9. Technical Stack

| Component | Technology |
|-----------|------------|
| Backend Framework | .NET 10 (Clean Architecture) |
| Frontend | Vue3 |
| Database | MySQL |
| ORM | Dapper (HandleWorker), Entity Framework patterns (API) |
| Message Queue | Kafka |
| API Gateway | YARP |
| Authentication | JWT |
| Logging | NLog |
| API Documentation | Swagger/OpenAPI |
| Architecture | Clean Architecture with shared layers |

---

## 10. Shared Library Structure

```
Shared/
├── BE.Domain/
│   ├── Entities/          # Domain entities (snake_case)
│   ├── Repos/             # IBaseRepo interfaces
│   └── Querys/            # SubmitModel
├── BE.Application/
│   ├── Services/          # Service implementations
│   └── Exceptions/         # BusinessException
├── BE.Application.Contracts/
│   ├── Interfaces/         # IService interfaces
│   └── Dtos/              # DTOs (snake_case)
├── BE.HostBase/
│   └── Extensions/         # DI registration, config
└── BE.Library/            # Utility services
```

---

## 11. Development Notes

### 11.1 Naming Convention
- **Properties**: snake_case (employee_id, full_name)
- **Entity Classes**: PascalCase (employee, customer)
- **MySQL Columns**: snake_case (direct mapping)
- **DTOs**: snake_case

### 11.2 Code Comments
Tất cả functions/phương thức phải có XML comment tiếng Việt có dấu.

### 11.3 Dependency Rules
- Application chỉ tham chiếu Domain (IRepo interface)
- Host chỉ tham chiếu Application.Contracts (IService interface)
- Không gọi trực tiếp implementation từ layer khác

---

## 12. Out of Scope (Future)

- Multi-tenant database per customer (provisioning)
- API Gateway authentication middleware
- ELK centralized logging
- Circuit breaker / Retry policy
- Kubernetes deployment
- API rate limiting

---

## 13. Acceptance Criteria

- [ ] 4 backend services chạy độc lập (Auth, Business, Order, HandleWorker)
- [ ] API Gateway route đúng các requests
- [ ] RESTful API đầy đủ CRUD cho 5 business entities
- [ ] Kafka message được publish khi tạo order
- [ ] HandleWorker consume và ghi ledger đúng
- [ ] JWT authentication hoạt động (login, register, Google)
- [ ] Swagger UI trên mỗi service
- [ ] NLog ghi log ra file
- [ ] Database schema đầy đủ như thiết kế
- [ ] Báo cáo đầy đủ