# Diagrams — sơ đồ minh hoạ

File này tập hợp các **Mermaid diagram** minh hoạ kiến trúc và dữ liệu. Mỗi diagram đứng độc lập, kèm ghi chú giải thích ngắn và tham chiếu code thực tế.

## Khi nào xem file này?

- Muốn có **cái nhìn thị giác nhanh** về kiến trúc trước khi đọc chi tiết
- Đang **chuẩn bị slide thuyết trình** hoặc tài liệu báo cáo
- Muốn in ra dán tường để nhìn tổng quan

## Quan hệ với tài liệu khác

| File | Nội dung |
|---|---|
| [`system-context.md`](system-context.md) | Sơ đồ C4 dạng ASCII (3 level: Context → Container → Deployment) |
| [`service-catalog.md`](service-catalog.md) | Catalog 7 thành phần chính, dạng bảng |
| [`communication.md`](communication.md) | Bảng REST endpoints + schema Kafka message |
| [`../01-business/workflows.md`](../01-business/workflows.md) | Sequence diagram ASCII cho 6 workflow (A-F) |
| [`../01-business/data-model.md`](../01-business/data-model.md) | ERD Mermaid đầy đủ 11 bảng |
| **`diagrams.md` (file này)** | Mermaid bổ sung: component overview, Kafka routing, state machines, k8s topology |

---

## 1. Component / system overview

Sơ đồ tổng thể các container và luồng kết nối giữa chúng. Mỗi **mũi tên liền** là HTTP/REST hoặc JDBC; **mũi tên đứt** là Kafka pub/sub.

```mermaid
flowchart TB
    User(["👤 Người dùng"])

    subgraph FE ["Frontend SPA"]
        SPA["Vue 3 + Vuetify + Pinia<br/>Port: 3000 dev · 80 k8s"]
    end

    subgraph GW ["API Gateway (reverse proxy)"]
        Gateway["ASP.NET HttpClient<br/>Port: 5000 dev · 80 k8s"]
    end

    subgraph APIS ["Backend APIs (ASP.NET)"]
        AuthAPI["AuthApi<br/>5001 · 5289 dotnet run"]
        BusinessAPI["BusinessApi<br/>5002 · 5119 dotnet run"]
        OrderAPI["OrderApi<br/>5003 · 5120 dotnet run"]
    end

    subgraph BG ["Background Workers (.NET Hosted)"]
        VW["VoucherWorker"]
        LW["LedgerWorker"]
    end

    subgraph STORE ["Data Layer"]
        M1[("MySQL master_db<br/>:3306")]
        M2[("MySQL business_db<br/>:3306")]
        KF{{"Kafka<br/>order-created · ledger-change"}}
    end

    User -->|"HTTPS"| SPA
    SPA -->|"fetch /Auth/*"| Gateway
    SPA -->|"fetch /Business/*"| Gateway
    SPA -->|"fetch /Order/*"| Gateway
    Gateway -->|"proxy"| AuthAPI
    Gateway -->|"proxy"| BusinessAPI
    Gateway -->|"proxy"| OrderAPI
    AuthAPI -->|"R/W"| M1
    BusinessAPI -->|"R/W"| M2
    OrderAPI -->|"R/W"| M2
    BusinessAPI -->|"publish ledger-change"| KF
    OrderAPI -->|"publish order-created<br/>+ ledger-change UPDATE"| KF
    KF -->|"consume"| VW
    KF -->|"consume"| LW
    VW -->|"INSERT Outward + Items"| M2
    LW -->|"INSERT Ledger +<br/>UPSERT LedgerDate + Closing"| M2
```

**Tổng cộng 11 container**: 1 frontend + 1 gateway + 3 API + 2 worker + 2 MySQL + 1 Kafka + 1 (browser/user). Trong k8s, FE/GW cũng là Deployment + Service riêng.

Xem thêm: [`system-context.md`](system-context.md) (C4 chi tiết), [`service-catalog.md`](service-catalog.md).

---

## 2. Kafka topic routing

Ai **produce** topic nào, ai **consume** topic nào. Hệ thống có **2 topic**, mỗi topic có 1 consumer group duy nhất.

```mermaid
flowchart LR
    subgraph PRODUCERS ["Producers"]
        BA["BusinessApi<br/>(Inward/Outward CRUD)"]
        OA["OrderApi<br/>(Order create/delete)"]
        VW["VoucherWorker<br/>(sau khi tạo Outward)"]
    end

    subgraph TOPICS ["Kafka Topics"]
        T1[/"order-created<br/>1 partition·auto.create"/]
        T2[/"ledger-change<br/>event_type: CREATE hoặc UPDATE"/]
    end

    subgraph CONSUMERS ["Consumers"]
        VWC["VoucherWorker<br/>group: voucher-worker-group"]
        LWC["LedgerWorker<br/>group: ledger-worker-group"]
    end

    OA -->|"publish"| T1
    T1 -->|"consume"| VWC
    VWC -->|"gọi OutwardService<br/>CreateAsync"| VW
    VW -->|"publish ledger-change"| T2

    BA -->|"publish CREATE/UPDATE"| T2
    OA -->|"publish UPDATE qty=0<br/>cascade delete order"| T2
    T2 -->|"consume"| LWC
```

**Đặc điểm**:
- Không dùng Kafka Schema Registry — message là JSON snake_case (`backend/Workers/Workers.Shared/Models/`)
- Publish qua `IProducer<string, string>` (key = entity id, value = JSON)
- Consumer đọc bằng `IConsumer<string, string>`, parse JSON, xử lý
- Offset commit **manual** sau khi xử lý thành công (tránh xử lý trùng khi crash)

Xem thêm: [`communication.md § 2. Giao tiếp bất đồng bộ — Kafka`](communication.md#2-giao-tiếp-bất-đồng-bộ--kafka).

---

## 3. Order lifecycle

Vòng đời 1 đơn hàng từ lúc tạo đến khi ledger được ghi (hoặc bị xóa cascade). Mỗi **transition** là 1 lần gọi API hoặc 1 lần Kafka message.

```mermaid
stateDiagram-v2
    [*] --> Created: POST /Order/api/orders

    Created: Order đã lưu DB<br/>(Order + OrderItems)<br/>status = PENDING
    Published: order-created<br/>đã publish Kafka<br/>(sync, fire-and-forget)
    VouchersGenerated: VoucherWorker đã tạo<br/>1 Outward / OrderItem<br/>publish ledger-change
    LedgerUpdated: LedgerWorker đã ghi<br/>ledger entry + upsert closing
    Deleted: Order đã xóa cascade<br/>(items + outward liên quan)<br/>publish ledger-change UPDATE qty=0

    Created --> Published: OrderService<br/>.PublishOrderCreatedMessage
    Published --> VouchersGenerated: VoucherWorker consume
    VouchersGenerated --> LedgerUpdated: LedgerWorker consume
    LedgerUpdated --> [*]: Hoàn tất

    Created --> Deleted: DELETE /orders/{id}<br/>(chưa kịp publish)
    Published --> Deleted: DELETE<br/>(trước khi VoucherWorker xử lý)
    VouchersGenerated --> Deleted: DELETE cascade<br/>(Worker chưa kịp ghi ledger)
    Deleted --> [*]: Đã xóa
```

**Code tham chiếu**:
- Tạo order: `backend/BE.Application/Services/Order/OrderService.cs:126-175`
- Publish `order-created`: `OrderService.cs:245-271`
- Cascade delete publish `ledger-change UPDATE qty=0`: `OrderService.RemoveAsync`

**Lưu ý race condition**: nếu user xóa order **trước khi** VoucherWorker kịp consume, các outward có thể đã được tạo nửa chừng → OrderService.RemoveAsync phải query outward liên quan và publish UPDATE qty=0 cho cả những outward đó.

---

## 4. Voucher (Inward / Outward) lifecycle

Vòng đời 1 phiếu nhập hoặc phiếu xuất. Khác với Order, voucher có thể **sửa** và **xóa** — mỗi thao tác đều publish `ledger-change` để worker đồng bộ lại sổ cái.

```mermaid
stateDiagram-v2
    [*] --> Created: POST /Business/api/inwards<br/>hoặc /outwards

    Created: Voucher đã lưu DB<br/>(Inward/Outward + Items)<br/>publish ledger-change<br/>event_type = CREATE
    LedgerSynced: LedgerWorker đã INSERT<br/>ledger entry + UPSERT<br/>ledger_date + closing
    Modified: Voucher đã UPDATE<br/>(qty/product/stock thay đổi)<br/>publish ledger-change UPDATE<br/>kèm old_qty/old_product/old_stock
    LedgerRebuilt: LedgerWorker đã REBUILD<br/>(DEL old entry, INSERT new)<br/>đồng bộ lại closing
    Deleted: Voucher đã xóa<br/>publish ledger-change UPDATE<br/>quantity=0

    Created --> LedgerSynced: LedgerWorker<br/>consume CREATE
    LedgerSynced --> Modified: PUT /vouchers/{id}
    Modified --> LedgerRebuilt: LedgerWorker<br/>consume UPDATE
    LedgerRebuilt --> LedgerSynced: Đã đồng bộ

    LedgerSynced --> Deleted: DELETE /vouchers/{id}
    Modified --> Deleted: DELETE
    Deleted --> [*]
```

**Tại sao cần `event_type` và `old_qty`?**

Vì sổ cái là **append-only** (chỉ INSERT, không UPDATE). Khi sửa voucher, worker không thể UPDATE ledger entry cũ — thay vào đó:
1. Insert ledger entry mới với giá trị mới
2. Insert ledger entry **đảo dấu** với `old_qty` để "trừ" entry cũ
3. Recompute `closing` (tồn cuối kỳ)

Nhờ vậy audit trail đầy đủ — biết được ledger entry nào từ phiếu nào, khi nào, với giá trị gì.

Xem thêm: [`../01-business/workflows.md`](../01-business/workflows.md) Workflow E (Sửa phiếu).

---

## 5. Kubernetes deployment topology

Triển khai trên minikube: mỗi service là 1 **Deployment + Service** trong namespace tương ứng. MySQL chạy **ngoài cluster** trên Windows host.

```mermaid
flowchart LR
    Browser(["🌐 User Browser<br/>http://ecom.local"])

    subgraph HOST ["Windows Host (ngoài cluster)"]
        Hosts["hosts file<br/>127.0.0.1 ecom.local"]
        MySQL[("MySQL Server<br/>:3306<br/>")]
    end

    Tunnel["minikube tunnel<br/>:80 → cluster:80"]

    subgraph K8S ["minikube cluster"]
        ING["ingress-nginx-controller<br/>namespace: ingress-nginx<br/>routes:<br/>/ → frontend<br/>/Auth,/Business,/Order → gateway<br/>/kibana → kibana"]

        subgraph NS_ECOM ["namespace: ecom"]
            FE["frontend<br/>(nginx + Vue dist)"]
            GW["api-gateway"]
            AUTH["auth-api"]
            BUS["business-api"]
            ORD["order-api"]
            VW["voucher-worker"]
            LW["ledger-worker"]
        end

        subgraph NS_LOG ["namespace: logging"]
            ES["elasticsearch"]
            LS["logstash"]
            Kibana["kibana"]
        end

        Kafka["kafka<br/>(KRaft, :9092/:9093)"]
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
    BUS -->|"host.minikube.internal:3306"| MySQL
    ORD -->|"host.minikube.internal:3306"| MySQL
    VW -->|"host.minikube.internal:3306"| MySQL
    LW -->|"host.minikube.internal:3306"| MySQL

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

**5 namespace trong cluster**:

| Namespace | Workloads | Mục đích |
|---|---|---|
| `ecom` | 7 Deployments + Services | Toàn bộ backend + frontend |
| `logging` | 3 Deployments (ES, LS, Kibana) | Centralized logging |
| `ingress-nginx` | 1 Deployment | Ingress controller |
| `kubernetes-dashboard` | (auto) | Quản lý cluster qua UI |
| `kube-system` | (auto) | System pods |

**Khác biệt với docker-compose**:
- docker-compose chạy 1 host, tất cả container dùng `localhost` → giao tiếp đơn giản
- k8s phân tán qua Pod IP → phải dùng Service (ClusterIP) + Ingress để routing
- MySQL **phải** chạy ngoài cluster vì image Bitnami đã bị xoá khỏi Docker Hub cuối 2025 (xem memory `project_bitnami_removal.md`)

Xem thêm: [`../03-deployment/k8s-deploy.md`](../03-deployment/k8s-deploy.md) (helm chart structure, scripts, troubleshooting).

---

## 6. Render & xuất diagram

Mermaid render tự động trên:
- **GitHub / GitLab** — preview trực tiếp khi xem file `.md`
- **VS Code** — cài extension `Markdown Preview Mermaid Support`
- **Obsidian / Typora** — render native
- **Online** — paste vào [mermaid.live](https://mermaid.live/) để export PNG/SVG

Diagram này dùng syntax Mermaid **v9+** (compatible với GitHub mặc định). Nếu IDE không render, copy nội dung trong khối ```` ```mermaid ```` ra [mermaid.live](https://mermaid.live/).