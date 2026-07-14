# Diagrams — sơ đồ minh hoạ kiến trúc (Chương 2)

File này tập hợp **13 sơ đồ minh hoạ** cho chương "Kiến trúc hệ thống", đánh số theo thứ tự từ tổng quan → chi tiết.

## Quy ước đánh số

Các diagram được đánh số **HÌNH 2.1 → HÌNH 2.13**, dùng để tham chiếu trong báo cáo đồ án.

## Khi nào xem file này?

- Đang viết **báo cáo đồ án** / **luận văn** và cần trích dẫn hình
- Đang **chuẩn bị slide thuyết trình** (export PNG/SVG từ Mermaid)
- Muốn có **cái nhìn thị giác nhanh** về kiến trúc trước khi đọc chi tiết

## Danh sách 13 hình

| Hình | Tiêu đề | Loại Mermaid | Mục đích |
|---|---|---|---|
| [2.1](#hinh-21-so-do-ngu-canh-he-thong--system-context-c4) | System Context C4 | flowchart | Tổng quan: ai tương tác với hệ thống |
| [2.2](#hinh-22-so-do-kien-truc-container--level-2-c4) | Container C4 | flowchart | Các container/process bên trong hệ thống |
| [2.3](#hinh-23-so-do-luong-request-tu-frontend-qua-apigateway-den-cac-backend-api) | Luồng request Frontend → Gateway → APIs | sequenceDiagram | Luồng đồng bộ qua HTTP |
| [2.4](#hinh-24-so-do-quan-he-thuc-the--erd) | ERD | erDiagram | Quan hệ giữa các thực thể chính |
| [2.5](#hinh-25-so-do-luong-request-qua-apigateway) | Luồng request qua ApiGateway | flowchart | Logic routing trong Gateway |
| [2.6](#hinh-26-so-do-luong-kafka-message-giua-cac-service-va-workers) | Kafka flow (topic-level) | flowchart | Producer/consumer cho 2 topic |
| [2.7](#hinh-27-so-do-bang-rest-api-endpoints) | Bảng REST API endpoints | flowchart | Phân nhóm endpoint theo service |
| [2.8](#hinh-28-so-do-luong-message-kafka) | Kafka flow chi tiết (sequence) | sequenceDiagram | Luồng message end-to-end |
| [2.9](#hinh-29-so-do-dependency-giua-cac-layer) | Backend layer dependency | flowchart | Clean Architecture layers |
| [2.10](#hinh-210-so-do-3-kich-ban-trien-khai) | 3 kịch bản triển khai | flowchart | Lựa chọn môi trường chạy |
| [2.11](#hinh-211-kien-truc-docker-compose) | Kiến trúc Docker Compose | flowchart | Triển khai flat trên 1 host |
| [2.12](#hinh-212-kien-truc-kubernetes-tren-minikube) | Kiến trúc Kubernetes minikube | flowchart | Triển khai phân tán trên cluster |
| [2.13](#hinh-213-so-do-cac-namespaces-va-services-trong-kubernetes) | Kubernetes namespaces + services | flowchart | Zoom vào các namespace |

---

## HÌNH 2.1: Sơ đồ ngữ cảnh hệ thống — System Context C4

C4 Level 1: thể hiện hệ thống như 1 **black box**, chỉ quan tâm **ai tương tác với hệ thống** chứ không quan tâm bên trong.

```mermaid
flowchart LR
    User(["👤 Người dùng<br/>(khách hàng / admin /<br/>nhân viên kho)"])
    System{{"THSPTIT<br/>DistributedSystem<br/><br/>Microservice bán hàng<br/>B2C + quản lý kho"}}

    User -->|"HTTPS<br/>(Browser / Mobile)"| System
    System -.->|"Response<br/>JSON"| User
```

**Phạm vi demo**:
- ✅ Có: web UI (SPA Vue 3) cho người dùng cuối
- ❌ Không có: tích hợp hệ thống ngoài (payment gateway, email service, SSO thật)
- ❌ Không có: app mobile native

**Xem thêm**: [`system-context.md`](system-context.md#level-1--system-context) — chi tiết C4 Level 1.

---

## HÌNH 2.2: Sơ đồ kiến trúc Container — Level 2 C4

C4 Level 2: mỗi **container** = 1 process chạy độc lập (service, worker, database). Đây là view quan trọng nhất khi triển khai.

```mermaid
flowchart TB
    User(["👤 Người dùng"])

    subgraph SYSTEM ["THSPTIT_DistributedSystem (hệ thống)"]

        subgraph FE ["Frontend layer"]
            SPA["Vue 3 + Vuetify SPA<br/>nginx<br/>:3000 dev · :80 k8s"]
        end

        subgraph EDGE ["Edge layer"]
            Gateway["ApiGateway<br/>ASP.NET + HttpClient<br/>:5000 dev · :80 k8s"]
        end

        subgraph APPS ["Application layer (4 API + 2 worker)"]
            AuthAPI["AuthApi<br/>:5001 · :5289"]
            BusinessAPI["BusinessApi<br/>:5002 · :5119"]
            OrderAPI["OrderApi<br/>:5003 · :5120"]
            VW["VoucherWorker<br/>(Hosted Service)"]
            LW["LedgerWorker<br/>(Hosted Service)"]
        end

        subgraph DATA ["Data layer"]
            M1[("MySQL master_db<br/>:3306")]
            M2[("MySQL business_db<br/>:3306")]
            KF{{"Kafka<br/>order-created · ledger-change"}}
        end

    end

    User -->|"HTTPS"| SPA
    SPA -->|"REST /Auth/*<br/>REST /Business/*<br/>REST /Order/*"| Gateway
    Gateway -->|"proxy"| AuthAPI
    Gateway -->|"proxy"| BusinessAPI
    Gateway -->|"proxy"| OrderAPI
    AuthAPI --> M1
    BusinessAPI --> M2
    OrderAPI --> M2
    BusinessAPI -->|"publish ledger-change"| KF
    OrderAPI -->|"publish order-created<br/>+ ledger-change UPDATE"| KF
    KF -->|"consume order-created"| VW
    KF -->|"consume ledger-change"| LW
    VW -->|"INSERT Outward + Items"| M2
    LW -->|"INSERT Ledger +<br/>UPSERT Closing"| M2
```

**Tổng cộng 11 container**: 1 FE + 1 GW + 3 API + 2 worker + 2 MySQL + 1 Kafka.

**Xem thêm**: [`system-context.md`](system-context.md#level-2--container), [`service-catalog.md`](service-catalog.md) — port, tech, trách nhiệm từng service.

---

## HÌNH 2.3: Sơ đồ luồng request từ Frontend qua ApiGateway đến các Backend API

Luồng xử lý **đồng bộ qua HTTP** cho 1 request bất kỳ (ví dụ: tạo đơn hàng).

```mermaid
sequenceDiagram
    autonumber
    actor User as Người dùng
    participant FE as Frontend<br/>(Vue)
    participant GW as ApiGateway<br/>(ASP.NET)
    participant API as Backend API<br/>(Business/Order/Auth)
    participant DB as MySQL

    User->>FE: Click "Tạo đơn hàng" + điền form
    FE->>FE: Validate client + lấy JWT từ localStorage
    FE->>GW: POST /Order/api/orders<br/>Authorization: Bearer <JWT>
    GW->>GW: Parse path → route "/Order/*" → OrderApi URL
    GW->>API: Forward POST + headers
    API->>API: [Authorize] verify JWT<br/>(filter middleware)
    API->>DB: BEGIN TRANSACTION
    API->>DB: INSERT order
    API->>DB: INSERT order_items
    API->>DB: COMMIT
    DB-->>API: order_id, items
    API->>API: Generate order_code (DH + seq)
    API->>API: publish order-created<br/>(fire-and-forget)
    API-->>GW: 201 Created + JSON body
    GW-->>FE: 201 Created + JSON body
    FE->>User: Hiển thị toast "Tạo thành công"<br/>redirect /orders
```

**Đặc điểm**:
- Gateway chỉ **forward**, không xử lý nghiệp vụ
- JWT được verify ở **Backend API** (không phải Gateway) — mỗi service có `[Authorize]` filter riêng
- Kafka publish là **fire-and-forget**: client không đợi worker xử lý

**Xem thêm**: [`communication.md § 1`](communication.md#1-giao-tiep-dong-bo--rest-api) — REST endpoints + auth header.

---

## HÌNH 2.4: Sơ đồ quan hệ thực thể — ERD

ERD rút gọn tập trung vào **luồng chính** (Order → Outward → Ledger). ERD đầy đủ 11 bảng xem tại [`../01-business/data-model.md`](../01-business/data-model.md#so-do-erd).

```mermaid
erDiagram
    CUSTOMERS ||--o{ ORDERS : "customer_id"
    STOCKS ||--o{ ORDERS : "stock_id"
    STOCKS ||--o{ OUTWARDS : "stock_id"
    PRODUCTS ||--o{ ORDER_ITEMS : "product_id"
    PRODUCTS ||--o{ OUTWARD_ITEMS : "product_id"
    PRODUCTS ||--o{ INWARD_ITEMS : "product_id"
    ORDERS ||--o{ ORDER_ITEMS : "order_id"
    ORDERS ||--o| OUTWARDS : "order_id (nullable)"
    INWARDS ||--o{ INWARD_ITEMS : "inward_id"
    OUTWARDS ||--o{ OUTWARD_ITEMS : "outward_id"
    INWARDS ||--o{ LEDGER : "reference_id<br/>reference_type=INWARD"
    OUTWARDS ||--o{ LEDGER : "reference_id<br/>reference_type=OUTWARD"
    PRODUCTS ||--o{ LEDGER : "product_id"
    STOCKS ||--o{ LEDGER : "stock_id"
    PRODUCTS ||--o{ LEDGER_CLOSING : "product_id"
    STOCKS ||--o{ LEDGER_CLOSING : "stock_id"

    ORDERS {
        VARCHAR order_id PK
        VARCHAR order_code UK "DH + sequence"
        VARCHAR customer_id FK
        VARCHAR stock_id FK
        DECIMAL total_amount
        VARCHAR status "PENDING|..."
        DATETIME order_date
    }
    ORDER_ITEMS {
        VARCHAR order_item_id PK
        VARCHAR order_id FK
        VARCHAR product_id FK
        INT quantity
        DECIMAL unit_price
    }
    OUTWARDS {
        VARCHAR outward_id PK
        VARCHAR outward_code UK "PX + sequence"
        VARCHAR order_id FK "nullable"
        VARCHAR stock_id FK
        DATETIME outward_date
    }
    OUTWARD_ITEMS {
        VARCHAR outward_item_id PK
        VARCHAR outward_id FK
        VARCHAR product_id FK
        INT quantity
        DECIMAL unit_price
    }
    LEDGER {
        VARCHAR ledger_id PK
        VARCHAR reference_id FK "inward/outward id"
        VARCHAR reference_type "INWARD|OUTWARD"
        VARCHAR product_id FK
        VARCHAR stock_id FK
        INT quantity "dương: nhập, âm: xuất"
        DATETIME transaction_date
    }
    LEDGER_CLOSING {
        VARCHAR closing_id PK
        VARCHAR product_id FK
        VARCHAR stock_id FK
        INT closing_quantity
        DATETIME closing_date
    }
```

**Quy tắc nghiệp vụ chính**:
- 1 Order → n OrderItem → 1 Outward (có thể) → n OutwardItem → 1 Ledger entry
- Ledger là **append-only**, không UPDATE entry cũ
- `LEDGER_CLOSING.closing_quantity` = tồn kho tại thời điểm `closing_date`

**Xem thêm**: [`../01-business/data-model.md`](../01-business/data-model.md) — schema đầy đủ 11 bảng, index, constraint.

---

## HÌNH 2.5: Sơ đồ luồng request qua ApiGateway

Logic **routing** bên trong ApiGateway: làm sao request `/Auth/*` đến đúng AuthApi, `/Business/*` đến BusinessApi?

```mermaid
flowchart TB
    FE["Frontend<br/>(axios interceptor gắn JWT)"]
    GW{"ApiGateway<br/>ProxyService.Route"}
    AuthBackend["AuthApi<br/>:5001 · :5289"]
    BusBackend["BusinessApi<br/>:5002 · :5119"]
    OrdBackend["OrderApi<br/>:5003 · :5120"]

    FE -->|"GET /Auth/login"| GW
    FE -->|"POST /Auth/register"| GW
    FE -->|"GET /Auth/me"| GW
    FE -->|"GET /Business/api/customers"| GW
    FE -->|"POST /Business/api/inwards"| GW
    FE -->|"GET /Order/api/orders"| GW
    FE -->|"POST /Order/api/orders"| GW

    GW -->|"startsWith /Auth/"| AuthBackend
    GW -->|"startsWith /Business/"| BusBackend
    GW -->|"startsWith /Order/"| OrdBackend

    AuthBackend -->|"return JSON"| GW
    BusBackend -->|"return JSON"| GW
    OrdBackend -->|"return JSON"| GW
    GW -->|"return JSON<br/>giữ nguyên status code"| FE
```

**Code routing** (`backend/ApiGateway/Program.cs`):
- Parse `request.Path` → match prefix `/Auth/`, `/Business/`, `/Order/`
- Lookup URL backend từ config (`Services__AuthApi`, `Services__BusinessApi`, `Services__OrderApi`)
- Copy method, headers, body → `HttpClient.SendAsync`
- Trả response nguyên bản về client

**Không có**: YARP. Không có: load balancing. Không có: circuit breaker.

**Xem thêm**: [`backend-codebase.md`](backend-codebase.md) — chi tiết ApiGateway ở §1 (Tổng quan solution) và §3 (chi tiết từng project).

---

## HÌNH 2.6: Sơ đồ luồng Kafka message giữa các service và workers

View **topic-level**: ai produce, ai consume cho 2 topic `order-created` và `ledger-change`.

```mermaid
flowchart LR
    subgraph PROD ["Producers"]
        BA["BusinessApi<br/>(Inward/Outward CRUD)"]
        OA["OrderApi<br/>(Order create/delete)"]
        VW["VoucherWorker<br/>(sau khi tạo Outward)"]
    end

    subgraph TOPICS ["Kafka Topics"]
        T1[/"order-created<br/>1 partition<br/>auto.create"/]
        T2[/"ledger-change<br/>event_type: CREATE|UPDATE"/]
    end

    subgraph CONS ["Consumers (1 group / topic)"]
        VWC["VoucherWorker<br/>group: voucher-worker-group"]
        LWC["LedgerWorker<br/>group: ledger-worker-group"]
    end

    OA -->|"publish"| T1
    T1 -->|"consume + commit offset"| VWC
    VWC -->|"tạo Outward"| VW
    VW -->|"publish"| T2

    BA -->|"publish CREATE/UPDATE"| T2
    OA -->|"publish UPDATE qty=0<br/>(cascade delete)"| T2
    T2 -->|"consume + commit offset"| LWC
```

**Đặc điểm**:
- Mỗi topic có **1 consumer group duy nhất** → message được xử lý đúng 1 lần (per group)
- Key = entity id (`order_id`, `ledger_change_id`) → đảm bảo thứ tự xử lý trong cùng partition
- Value = JSON snake_case (`backend/Workers/Workers.Shared/Models/`)
- Offset commit **manual** sau khi xử lý thành công → tránh xử lý trùng khi worker crash

**Xem thêm**: [`communication.md § 2`](communication.md#2-giao-tiep-bat-dong-bo--kafka) — schema message chi tiết.

---

## HÌNH 2.7: Sơ đồ bảng REST API endpoints

Phân nhóm endpoint theo **3 service chính**. Tổng cộng ~36 endpoint.

```mermaid
flowchart TB
    subgraph AUTH ["AuthApi (5 endpoints)"]
        A1["POST /Auth/login"]
        A2["POST /Auth/register"]
        A3["GET  /Auth/me"]
        A4["POST /Auth/refresh-token"]
        A5["POST /Auth/google (stub)"]
    end

    subgraph BUS ["BusinessApi (~25 endpoints)"]
        B1["Customer CRUD<br/>/Business/api/customers"]
        B2["Product CRUD<br/>/Business/api/products"]
        B3["Stock CRUD<br/>/Business/api/stocks"]
        B4["Inward CRUD<br/>/Business/api/inwards"]
        B5["Outward CRUD<br/>/Business/api/outwards"]
        B6["ProductPrices<br/>/Business/api/productprices"]
        B7["Health<br/>/health"]
    end

    subgraph ORD ["OrderApi (6 endpoints)"]
        O1["POST /Order/api/orders"]
        O2["GET  /Order/api/orders/{id}"]
        O3["GET  /Order/api/orders (paging)"]
        O4["PUT  /Order/api/orders/{id}"]
        O5["DELETE /Order/api/orders/{id}"]
        O6["GET  /Order/health"]
    end

    AUTH -.->|"AuthRequired: optional"| BUS
    BUS -.->|"AuthRequired: yes"| ORD
```

**Quy ước URL**: prefix theo service → gateway dựa vào prefix để route (`HÌNH 2.5`).

**Auth**: mọi endpoint ngoài `/Auth/login` và `/Auth/register` đều yêu cầu `Authorization: Bearer <JWT>`.

**Xem thêm**: [`communication.md § 1.4`](communication.md#14-bang-endpoints-day-du) — bảng đầy đủ 36 endpoint với method, path, auth.

---

## HÌNH 2.8: Sơ đồ luồng message Kafka

Sequence diagram chi tiết cho 1 message đi qua hệ thống — minh hoạ **eventual consistency**.

```mermaid
sequenceDiagram
    autonumber
    participant OrderApi
    participant Kafka
    participant VoucherWorker
    participant BusinessDB as MySQL business_db
    participant LedgerWorker

    Note over OrderApi: User POST /Order/api/orders
    OrderApi->>BusinessDB: BEGIN TX
    OrderApi->>BusinessDB: INSERT orders, order_items
    OrderApi->>BusinessDB: COMMIT
    OrderApi->>Kafka: produce order-created<br/>key=order_id, value=JSON
    OrderApi-->>OrderApi: 201 Created (return ngay)

    Note over Kafka,VoucherWorker: Async (worker poll sau vài giây)
    Kafka->>VoucherWorker: consume order-created
    VoucherWorker->>BusinessDB: SELECT order_items
    VoucherWorker->>BusinessDB: INSERT outwards, outward_items
    VoucherWorker->>Kafka: produce ledger-change (CREATE)
    VoucherWorker->>Kafka: commit offset

    Note over Kafka,LedgerWorker: Tiếp tục async
    Kafka->>LedgerWorker: consume ledger-change
    LedgerWorker->>BusinessDB: INSERT ledger entry
    LedgerWorker->>BusinessDB: UPSERT ledger_date
    LedgerWorker->>BusinessDB: UPSERT ledger_closing
    LedgerWorker->>Kafka: commit offset
```

**Đặc điểm**:
- **OrderApi → 201 ngay** — không đợi worker xử lý (latency thấp cho client)
- VoucherWorker + LedgerWorker chạy **async** — có thể mất vài giây
- Nếu worker crash giữa chừng: offset chưa commit → message được redelivery
- Audit trail đầy đủ vì ledger là append-only

**Xem thêm**: [`../01-business/workflows.md`](../01-business/workflows.md) Workflow A → B → C (full text version).

---

## HÌNH 2.9: Sơ đồ dependency giữa các layer

Clean Architecture: project nào tham chiếu project nào. **Quy tắc cứng**: dependency chỉ đi từ ngoài vào trong (Host → Application → Domain).

```mermaid
flowchart TB
    subgraph HOST ["BE.Host (entry point)"]
        H1[ApiGateway]
        H2[AuthApi]
        H3[BusinessApi]
        H4[OrderApi]
        H5[LedgerWorker]
        H6[VoucherWorker]
    end

    subgraph IMPL ["BE.Application (service implementations)"]
        I1[AuthService]
        I2[CustomerService]
        I3[ProductService]
        I4[StockService]
        I5[InwardService]
        I6[OutwardService]
        I7[OrderService]
        I8[LedgerService]
    end

    subgraph CONTRACT ["BE.Application.Contracts (interfaces + DTOs)"]
        C1[IAuthService · IAuthRepo]
        C2[ICustomerService · ICustomerRepo]
        C3[IOrderService · IOrderRepo]
        C4[IInwardService · IInwardRepo]
        C5[IOutwardService · IOutwardRepo]
        C6[ILedgerService · ILedgerRepo]
        C7["DTOs (CreateCustomerDto,<br/>CreateOrderDto, ...)"]
    end

    subgraph DOMAIN ["BE.Domain (entities + repo interfaces)"]
        D1[UserEntity · CustomerEntity<br/>ProductEntity · StockEntity]
        D2[InwardEntity · OutwardEntity<br/>OrderEntity · OrderItemEntity]
        D3[LedgerEntity · LedgerDateEntity<br/>ClosingEntity]
        D4[IUserRepo · ICustomerRepo<br/>IProductRepo · IStockRepo]
        D5[IInwardRepo · IOutwardRepo<br/>IOrderRepo · IOrderItemRepo]
        D6[ILedgerRepo · ILedgerDateRepo<br/>IClosingRepo]
    end

    subgraph MYSQL ["BE.Domain.Mysql (repo implementations)"]
        M1[UserRepo · CustomerRepo<br/>ProductRepo · StockRepo]
        M2[InwardRepo · OutwardRepo<br/>OrderRepo · OrderItemRepo]
        M3[LedgerRepo · LedgerDateRepo<br/>ClosingRepo]
        M4["Dapper + MySqlConnector<br/>(BaseRepo, DapperRepo)"]
    end

    H1 --> CONTRACT
    H1 --> IMPL
    H2 --> CONTRACT
    H2 --> IMPL
    H3 --> CONTRACT
    H3 --> IMPL
    H4 --> CONTRACT
    H4 --> IMPL
    H5 --> CONTRACT
    H5 --> IMPL
    H6 --> CONTRACT
    H6 --> IMPL

    IMPL --> CONTRACT
    CONTRACT --> DOMAIN
    IMPL --> DOMAIN
    MYSQL --> DOMAIN
    M4 --> M1
    M4 --> M2
    M4 --> M3
```

**Quy tắc**:
- **BE.Domain** không tham chiếu gì (trừ BE.Domain.Share cho models share)
- **BE.Application.Contracts** chỉ tham chiếu **BE.Domain** (cho entity + repo interface)
- **BE.Application** tham chiếu **BE.Application.Contracts** + **BE.Domain**
- **BE.Domain.Mysql** tham chiếu **BE.Domain** (impl repo interface)
- **BE.Host** (mỗi API/worker) tham chiếu tất cả các layer trên + **Workers.Shared** (cho Kafka helpers)

**Xem thêm**: [`backend-codebase.md`](backend-codebase.md) — chi tiết từng project, naming convention.

---

## HÌNH 2.10: Sơ đồ 3 kịch bản triển khai

Lựa chọn môi trường chạy hệ thống cho từng mục đích.

```mermaid
flowchart LR
    Q{Mục đích?}
    Q -->|"Dev nhanh<br/>(~5 phút)"| S1["Kịch bản 1:<br/>Docker Compose"]
    Q -->|"Debug code<br/>C#/Vue"| S2["Kịch bản 2:<br/>dotnet run + npm run dev"]
    Q -->|"Demo production-like<br/>(~20 phút)"| S3["Kịch bản 3:<br/>Kubernetes minikube"]

    S1 -->|"9 container<br/>MySQL + Kafka + 6 service<br/>file: backend/docker-compose.yml"| S1D["docker compose up -d"]
    S2 -->|"MySQL + Kafka qua Docker<br/>+ dotnet run từng service<br/>+ npm run dev"| S2D["6 terminal riêng"]
    S3 -->|"7 Deployment<br/>3 namespace<br/>+ Ingress + ELK"| S3D["./infra/scripts/all.sh<br/>+ minikube tunnel"]

    S1D -.->|"Ưu: 1 lệnh, đủ worker<br/>Nhược: không debug code"| Note1[📝]
    S2D -.->|"Ưu: hot reload<br/>Nhược: phải chạy nhiều terminal"| Note2[📝]
    S3D -.->|"Ưu: giống production<br/>Nhược: tốn RAM + setup lâu"| Note3[📝]
```

**Ma trận chọn kịch bản**:

| Mục đích | Kịch bản |
|---|---|
| Sửa code backend nhỏ, debug | 2 (dotnet run) |
| Sửa code frontend, hot reload | 2 (npm run dev) |
| Test event-driven flow (Kafka) | 1 (docker compose) |
| Demo cho giảng viên / báo cáo | 3 (k8s) |
| Test ingress / scale / ELK | 3 (k8s) |

**Xem thêm**: [`../03-deployment/README.md`](../03-deployment/README.md) — bảng so sánh chi tiết.

---

## HÌNH 2.11: Kiến trúc Docker Compose

Triển khai **flat** trên 1 host: tất cả container dùng chung network `ecom_default`, giao tiếp qua `localhost:port`.

```mermaid
flowchart TB
    subgraph HOST ["Windows / macOS / Linux host"]
        Browser["Browser<br/>localhost:3000"]

        subgraph NET ["Docker network: ecom_default"]
            C1["ecom-mysql-master<br/>:3306<br/>master_db"]
            C2["ecom-mysql-business<br/>:3306<br/>business_db"]
            C3["ecom-zookeeper<br/>(cho Kafka)"]
            C4["ecom-kafka<br/>:9092 internal · :9093 host"]
            C5["ecom-api-gateway<br/>:5000"]
            C6["ecom-auth-api<br/>:5001"]
            C7["ecom-business-api<br/>:5002"]
            C8["ecom-order-api<br/>:5003"]
            C9["ecom-voucher-worker"]
            C10["ecom-ledger-worker"]
        end

        FE["frontend container?<br/>(không có — chạy npm run dev ngoài Docker)"]
    end

    Browser -->|"localhost:3000"| FE
    FE -->|"axios → localhost:5000"| C5
    Browser -.->|"test trực tiếp"| C5

    C5 -->|"localhost:5001"| C6
    C5 -->|"localhost:5002"| C7
    C5 -->|"localhost:5003"| C8

    C6 --> C1
    C7 --> C2
    C8 --> C2
    C9 --> C2
    C10 --> C2

    C7 -.->|"localhost:9093"| C4
    C8 -.->|"localhost:9093"| C4
    C4 -.->|"subscribe"| C9
    C4 -.->|"subscribe"| C10
    C3 -.->|"control"| C4
```

**Lưu ý**:
- **Không có frontend container** trong docker-compose — frontend chạy qua `npm run dev` ở ngoài (Vite proxy trỏ vào gateway port 5000)
- **9 container** được khởi động, mỗi container đều có thể truy cập `localhost`
- **Kafka dual listener**: `:9092` cho container-to-container, `:9093` cho host-to-container
- **Volume mount** `backend/Scripts` → `/docker-entrypoint-initdb.d` để tự động chạy `init.sql` lần đầu

**File**: `backend/docker-compose.yml`

**Xem thêm**: [`../03-deployment/local-dev.md § 1`](../03-deployment/local-dev.md#1-docker-compose-khuyen-nghi-cho-dev).

---

## HÌNH 2.12: Kiến trúc Kubernetes trên minikube

Triển khai **phân tán** trên cluster: mỗi service là 1 Deployment + Service trong namespace, giao tiếp qua ClusterIP + Ingress.

```mermaid
flowchart LR
    Browser(["🌐 User Browser<br/>http://ecom.local"])

    subgraph HOST ["Windows Host (ngoài cluster)"]
        Hosts["C:\Windows\System32\drivers\etc\hosts<br/>127.0.0.1 ecom.local"]
        MySQL[("MySQL Server<br/>:3306<br/>Mysql!110720")]
    end

    Tunnel["minikube tunnel<br/>:80 → cluster:80"]

    subgraph K8S ["minikube cluster"]
        ING["ingress-nginx-controller<br/>namespace: ingress-nginx"]

        subgraph NS_ECOM ["namespace: ecom (7 workloads)"]
            FE["Pod: frontend<br/>(nginx + Vue dist)"]
            GW["Pod: api-gateway"]
            AUTH["Pod: auth-api"]
            BUS["Pod: business-api"]
            ORD["Pod: order-api"]
            VW["Pod: voucher-worker"]
            LW["Pod: ledger-worker"]
        end

        subgraph NS_LOG ["namespace: logging (3 workloads)"]
            ES["Pod: elasticsearch"]
            LS["Pod: logstash"]
            Kibana["Pod: kibana"]
        end

        Kafka["Pod: kafka<br/>(KRaft, :9092/:9093)"]
    end

    Browser -->|"ecom.local"| Hosts
    Hosts --> Tunnel
    Tunnel --> ING
    ING -->|"path /"| FE
    ING -->|"path /Auth,<br/>/Business, /Order"| GW
    ING -->|"path /kibana"| Kibana
    GW -->|"ClusterIP svc"| AUTH
    GW -->|"ClusterIP svc"| BUS
    GW -->|"ClusterIP svc"| ORD
    AUTH -->|"host.minikube.internal:3306"| MySQL
    BUS --> MySQL
    ORD --> MySQL
    VW --> MySQL
    LW --> MySQL
    BUS -->|"publish"| Kafka
    ORD -->|"publish"| Kafka
    Kafka -->|"consume"| VW
    Kafka -->|"consume"| LW
    AUTH -.->|"stdout"| LS
    BUS -.->|"stdout"| LS
    ORD -.->|"stdout"| LS
    GW -.->|"stdout"| LS
    FE -.->|"stdout"| LS
    VW -.->|"stdout"| LS
    LW -.->|"stdout"| LS
    LS --> ES
    Kibana --> ES
```

**Khác biệt so với Docker Compose** (`HÌNH 2.11`):
- **Pod IP động** → phải dùng Service (ClusterIP) để giao tiếp
- **Ingress** thay cho reverse proxy ngoài
- **MySQL ngoài cluster** (host.minikube.internal:3306) — vì Bitnami image đã bị xoá khỏi Docker Hub cuối 2025
- **ELK stack** (Elasticsearch + Logstash + Kibana) thay cho log file
- **Helm umbrella chart** `ecom-stack` quản lý 7 subchart local + 1 Kafka custom

**Xem thêm**: [`../03-deployment/k8s-deploy.md`](../03-deployment/k8s-deploy.md).

---

## HÌNH 2.13: Sơ đồ các namespaces và services trong Kubernetes

Zoom vào cluster: liệt kê **Service + Deployment** theo từng namespace.

```mermaid
flowchart TB
    subgraph MINIKUBE ["minikube cluster"]
        subgraph NS_ING ["namespace: ingress-nginx"]
            ING_SVC["Service: ingress-nginx-controller<br/>(NodePort 80·443)"]
            ING_DEP["Deployment: ingress-nginx-controller<br/>1 replica"]
        end

        subgraph NS_ECOM ["namespace: ecom"]
            FE_SVC["Service: frontend<br/>(ClusterIP)"]
            FE_DEP["Deployment: frontend<br/>1 replica · nginx + Vue"]
            GW_SVC["Service: api-gateway<br/>(ClusterIP)"]
            GW_DEP["Deployment: api-gateway<br/>1 replica · ASP.NET"]
            AUTH_SVC["Service: auth-api<br/>(ClusterIP)"]
            AUTH_DEP["Deployment: auth-api<br/>1 replica"]
            BUS_SVC["Service: business-api<br/>(ClusterIP)"]
            BUS_DEP["Deployment: business-api<br/>1 replica"]
            ORD_SVC["Service: order-api<br/>(ClusterIP)"]
            ORD_DEP["Deployment: order-api<br/>1 replica"]
            VW_SVC["Service: voucher-worker<br/>(ClusterIP, headless)"]
            VW_DEP["Deployment: voucher-worker<br/>1 replica"]
            LW_SVC["Service: ledger-worker<br/>(ClusterIP, headless)"]
            LW_DEP["Deployment: ledger-worker<br/>1 replica"]
            KAFKA_SVC["Service: kafka<br/>(ClusterIP)"]
            KAFKA_STS["StatefulSet: kafka<br/>(1 replica · KRaft)"]
        end

        subgraph NS_LOG ["namespace: logging"]
            ES_SVC["Service: elasticsearch<br/>(ClusterIP)"]
            ES_STS["StatefulSet: elasticsearch<br/>1 replica · 2Gi RAM"]
            LS_SVC["Service: logstash<br/>(ClusterIP)"]
            LS_DEP["Deployment: logstash<br/>1 replica"]
            KIB_SVC["Service: kibana<br/>(ClusterIP)"]
            KIB_DEP["Deployment: kibana<br/>1 replica"]
        end
    end

    subgraph HOST ["Windows Host"]
        HOSTS["hosts file"]
        MYSQL[("MySQL<br/>:3306")]
    end

    HOSTS -->|"minikube tunnel :80"| ING_SVC
    ING_SVC -->|"path /"| FE_SVC
    ING_SVC -->|"path /Auth,/Business,/Order"| GW_SVC
    ING_SVC -->|"path /kibana"| KIB_SVC

    GW_SVC --> AUTH_SVC
    GW_SVC --> BUS_SVC
    GW_SVC --> ORD_SVC

    FE_SVC --- FE_DEP
    GW_SVC --- GW_DEP
    AUTH_SVC --- AUTH_DEP
    BUS_SVC --- BUS_DEP
    ORD_SVC --- ORD_DEP
    VW_SVC --- VW_DEP
    LW_SVC --- LW_DEP
    KAFKA_SVC --- KAFKA_STS
    ES_SVC --- ES_STS
    LS_SVC --- LS_DEP
    KIB_SVC --- KIB_DEP

    AUTH_SVC -->|"host.minikube.internal:3306"| MYSQL
    BUS_SVC --> MYSQL
    ORD_SVC --> MYSQL
    VW_SVC --> MYSQL
    LW_SVC --> MYSQL

    LS_DEP -->|"gửi log"| ES_SVC
    KIB_DEP -->|"query"| ES_SVC
    BUS_SVC -.->|"publish"| KAFKA_SVC
    ORD_SVC -.->|"publish"| KAFKA_SVC
    KAFKA_SVC -.->|"consume"| VW_SVC
    KAFKA_SVC -.->|"consume"| LW_SVC
```

**5 namespace tổng cộng**:

| Namespace | Workloads | Mục đích |
|---|---|---|
| `ecom` | 7 Deployment + 7 Service + 1 StatefulSet (kafka) | Toàn bộ backend + frontend |
| `logging` | 2 Deployment (LS, Kibana) + 1 StatefulSet (ES) | ELK stack |
| `ingress-nginx` | 1 Deployment + 1 Service | Ingress controller |
| `kubernetes-dashboard` | (auto từ minikube addons) | UI quản lý cluster |
| `kube-system` | (auto) | System pods |

**Service type**:
- `ClusterIP` (default) — chỉ truy cập trong cluster
- `NodePort` — ingress-nginx dùng để nhận traffic từ `minikube tunnel`
- Worker + Kafka dùng **headless ClusterIP** (`clusterIP: None`) để client kết nối trực tiếp đến Pod IP

**Xem thêm**: [`../03-deployment/k8s-deploy.md § 8.1`](../03-deployment/k8s-deploy.md#81-namespaces), [`../03-deployment/k8s-deploy.md § 8.2`](../03-deployment/k8s-deploy.md#82-services-trong-namespace-ecom).

---

## Render & xuất diagram

Mermaid render tự động trên:
- **GitHub / GitLab** — preview trực tiếp khi xem file `.md`
- **VS Code** — cài extension `Markdown Preview Mermaid Support`
- **Obsidian / Typora** — render native
- **Online** — paste vào [mermaid.live](https://mermaid.live/) để export PNG/SVG

Diagram dùng syntax Mermaid **v9+** (compatible GitHub mặc định). Nếu IDE không render, copy nội dung trong khối ```` ```mermaid ```` ra [mermaid.live](https://mermaid.live/).

**Trích dẫn trong báo cáo**:

> HÌNH 2.X: tiêu đề (Nguồn: tài liệu đồ án, 2026)

Mỗi hình trong file này có anchor riêng (link trong bảng danh sách ở đầu file), có thể link thẳng tới từng hình trong báo cáo HTML/Markdown.
