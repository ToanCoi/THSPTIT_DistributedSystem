# PLAN: Microservice Application - Implementation

## Phase 1: Setup Infrastructure (Day 1)

### 1.1 Project Structure
- [ ] Tạo solution `THSPTIT_DistributedSystem.sln`
- [ ] Tạo thư mục `backend/` với cấu trúc:
  ```
  backend/
  ├── AuthApi/
  ├── BusinessApi/
  ├── OrderApi/
  ├── HandleWorker/
  ├── ApiGateway/
  └── Shared/
      ├── BE.Domain/
      ├── BE.Domain.Mysql/
      ├── BE.Application/
      ├── BE.Application.Contracts/
      ├── BE.HostBase/
      └── BE.Library/
  ```

### 1.2 Setup AuthApi
- [ ] Tạo project AuthApi (net10.0)
- [ ] Tham chiếu Shared libraries
- [ ] Cấu hình Program.cs với JWT, Swagger, NLog
- [ ] Tạo Controller: AuthController
  - POST /Auth/login
  - POST /Auth/register
  - POST /Auth/google
- [ ] Tạo User entity + Repository
- [ ] Implement JWT service
- [ ] Test: Swagger login/register

### 1.3 Setup BusinessApi
- [ ] Tạo project BusinessApi (net10.0)
- [ ] Tham chiếu Shared libraries
- [ ] Cấu hình Program.cs với Swagger, NLog, MySQL
- [ ] Tạo 5 Controllers:
  - CustomerController (CRUD)
  - ProductController (CRUD)
  - StockController (Read)
  - InwardController (Create, Read)
  - OutwardController (Create, Read)
- [ ] Tạo entities: customer, product, stock, inward, outward
- [ ] Implement services và repositories
- [ ] Test: Swagger CRUD operations

### 1.4 Setup OrderApi
- [ ] Tạo project OrderApi (net10.0)
- [ ] Tham chiếu Shared libraries
- [ ] Cấu hình Program.cs với Swagger, NLog, MySQL, Kafka Producer
- [ ] Tạo OrderController
  - GET /api/orders
  - POST /api/orders (tạo đơn + publish Kafka)
- [ ] Tạo entities: order, order_item
- [ ] Implement Kafka producer service
- [ ] Test: Swagger create order + verify Kafka message

### 1.5 Setup HandleWorker
- [ ] Tạo project HandleWorker (net10.0 worker service)
- [ ] Tham chiếu Shared libraries
- [ ] Cấu hình Program.cs với Kafka Consumer, NLog, MySQL
- [ ] Implement LedgerService (Dapper)
- [ ] Implement Kafka consumer background service
- [ ] Test: Consume message + write ledger tables

### 1.6 Setup ApiGateway
- [ ] Tạo project ApiGateway (net10.0)
- [ ] Cấu hình YARP reverse proxy
- [ ] Route: /Auth/* → AuthApi:5001
- [ ] Route: /Business/* → BusinessApi:5002
- [ ] Route: /Order/* → OrderApi:5003
- [ ] Test: Swagger on Gateway + route requests

---

## Phase 2: Database Setup (Day 1-2)

### 2.1 MySQL Database
- [ ] Tạo database `master_db`
  - Table: users
- [ ] Tạo database `business_db`
  - Tables: customers, products, stocks, inwards, outwards, orders, order_items
  - Tables: led_inventory_item_ledger, led_inventory_item_ledger_date, led_inventory_item_ledger_closing

### 2.2 Kafka Setup
- [ ] Cài đặt Kafka (hoặc dùng Docker)
- [ ] Tạo topic `order-created`

---

## Phase 3: Business Logic Implementation (Day 2-5)

### 3.1 Auth Module
- [ ] User registration với password hash (BCrypt)
- [ ] User login → JWT token
- [ ] Google OAuth integration (optional cho demo)
- [ ] JWT validation middleware

### 3.2 Business Modules
- [ ] Customer: CRUD operations
- [ ] Product: CRUD operations
- [ ] Stock: List stocks
- [ ] Inward: Tạo phiếu nhập kho
- [ ] Outward: Tạo phiếu xuất kho

### 3.3 Order Module
- [ ] Create order với order_items
- [ ] Calculate total_amount
- [ ] Publish to Kafka topic
- [ ] Get orders (list + detail)

### 3.4 Ledger Worker
- [ ] Kafka consumer subscribe `order-created`
- [ ] Parse order message
- [ ] Insert led_inventory_item_ledger
- [ ] Upsert led_inventory_item_ledger_date
- [ ] Update led_inventory_item_ledger_closing

---

## Phase 4: Integration & Testing (Day 5-10)

### 4.1 Integration Tests
- [ ] Test: Auth login → get JWT → access Business API
- [ ] Test: Create Customer → Create Product → Create Order
- [ ] Test: Order created → Kafka message → Ledger updated
- [ ] Test: API Gateway routing correct

### 4.2 Documentation
- [ ] Swagger on each service
- [ ] API documentation
- [ ] Update SPEC.md với actual URLs

---

## Phase 5: Report & Demo (Day 10-14)

### 5.1 Report
- [ ] Mô tả bài toán
- [ ] Kiến trúc hệ thống (sơ đồ)
- [ ] Mô tả từng microservice
- [ ] Thiết kế RESTful API
- [ ] Luồng message queue (flow diagram)
- [ ] Kết quả thử nghiệm
- [ ] Hướng dẫn chạy

### 5.2 Demo Preparation
- [ ] Docker Compose cho tất cả services
- [ ] Test end-to-end flow
- [ ] Chuẩn bị demo script

---

## Task Dependencies

```
Day 1: Setup
├── Setup Project Structure
├── Setup AuthApi → Test login/register
├── Setup BusinessApi → Test CRUD
├── Setup OrderApi → Test create order
└── Setup HandleWorker → Test consume message

Day 2-5: Business Logic
├── Auth: JWT implementation
├── Business: Full CRUD for all entities
├── Order: Kafka producer
└── HandleWorker: Ledger logic

Day 5-10: Integration
├── API Gateway routing
├── End-to-end testing
└── Fix bugs

Day 10-14: Report & Demo
├── Write report
├── Docker Compose
└── Demo
```

---

## Quick Commands

```bash
# Build all
dotnet build backend/THSPTIT_DistributedSystem.sln

# Run services (separate terminals)
dotnet run --project backend/AuthApi/AuthApi.csproj
dotnet run --project backend/BusinessApi/BusinessApi.csproj
dotnet run --project backend/OrderApi/OrderApi.csproj
dotnet run --project backend/HandleWorker/HandleWorker.csproj
dotnet run --project backend/ApiGateway/ApiGateway.csproj

# Swagger URLs
http://localhost:5001/swagger  # Auth
http://localhost:5002/swagger  # Business
http://localhost:5003/swagger  # Order

# Gateway URL
http://localhost:5000/Auth/login
http://localhost:5000/Business/customers
http://localhost:5000/Order/orders
```

---

## Notes

- Multi-tenant database provisioning để after
- ELK logging để after
- Circuit breaker để after
- Kubernetes deployment để after