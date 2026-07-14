# Luồng nghiệp vụ chính

Tài liệu này mô tả **6 luồng nghiệp vụ** chính bằng sequence diagram. Mỗi luồng đi kèm file:line tham chiếu trong code.

## Tổng quan 6 luồng

| Workflow | Mô tả | Service liên quan |
|---|---|---|
| **A** | Tạo đơn hàng | OrderApi |
| **B** | Sinh phiếu xuất từ đơn | OrderApi → VoucherWorker |
| **C** | Ghi sổ cái | VoucherWorker/Inward/Outward → LedgerWorker |
| **D** | Tạo phiếu nhập thủ công | BusinessApi |
| **E** | Sửa phiếu nhập/xuất | BusinessApi → LedgerWorker |
| **F** | Xóa đơn hàng (cascade) | OrderApi → LedgerWorker |

---

## Workflow A — Tạo đơn hàng

```
Client              OrderApi           MySQL            Kafka
  │                    │                 │                │
  │ POST /orders       │                 │                │
  ├───────────────────►│                 │                │
  │                    │                 │                │
  │                    │ INSERT order    │                │
  │                    ├────────────────►│                │
  │                    │                 │                │
  │                    │ INSERT items    │                │
  │                    ├────────────────►│                │
  │                    │                 │                │
  │                    │ GenerateOrderCode               │
  │                    │ (DH + sequence) │                │
  │                    │                 │                │
  │                    │ publish order-created            │
  │                    ├────────────────────────────────►│
  │                    │                 │                │
  │ 201 Created        │                 │                │
  │◄───────────────────┤                 │                │
```

**Code**: `OrdersController.Create` → `OrderService.CreateAsync`
- Controller: `backend/OrderApi/Controllers/OrdersController.cs:69-74`
- Service: `backend/BE.Application/Services/Order/OrderService.cs:126-175`
- Publish Kafka: `OrderService.PublishOrderCreatedMessage` (line 245-271)
- Mã đơn: `OrderService.GenerateOrderCodeAsync` (line 276-280)

**Request**:
```json
POST /Order/api/orders
{
  "customer_id": "guid",
  "stock_id": "guid",
  "items": [
    { "product_id": "guid", "quantity": 5, "unit_price": 100000 }
  ]
}
```

**Response 201**:
```json
{
  "order_id": "guid",
  "order_code": "DH7",
  "customer_id": "guid",
  "stock_id": "guid",
  "total_amount": 500000,
  "status": "PENDING",
  "order_date": "2026-07-14T...",
  "items": [...]
}
```

**Message gửi Kafka `order-created`**:
```json
{
  "order_id": "guid",
  "customer_id": "guid",
  "stock_id": "guid",
  "order_code": "DH7",
  "items": [
    { "product_id": "guid", "quantity": 5, "unit_price": 100000 }
  ],
  "timestamp": "2026-07-14T..."
}
```

→ Luồng tiếp theo: **[Workflow B](#workflow-b--sinh-phiếu-xuất-từ-đơn)**

---

## Workflow B — Sinh phiếu xuất từ đơn

```
                Kafka        VoucherWorker       OutwardService      MySQL         Kafka
                  │                │                    │              │             │
                  │ consume order- │                    │              │             │
                  │    created     │                    │              │             │
                  │───────────────►│                    │              │             │
                  │                │ For each item:     │              │             │
                  │                │ CreateAsync(dto)   │              │             │
                  │                ├───────────────────►│              │             │
                  │                │                    │ INSERT       │             │
                  │                │                    │  outward     │             │
                  │                │                    ├─────────────►│             │
                  │                │                    │              │             │
                  │                │                    │ publish      │             │
                  │                │                    │  ledger-     │             │
                  │                │                    │  change      │             │
                  │                │                    ├──────────────────────────►│
                  │                │                    │              │             │
                  │                │ Return OutwardDto  │              │             │
                  │                │◄───────────────────┤              │             │
```

**Code**:
- Worker: `backend/Workers/VoucherWorker/Program.cs:124-164` (`VoucherKafkaConsumer.ProcessMessageAsync`)
- Service được gọi: `OutwardService.CreateAsync` (`backend/BE.Application/Services/Outward/OutwardService.cs`)

**Lưu ý quan trọng**:
- VoucherWorker **KHÔNG publish Kafka `ledger-change`** ở worker. Việc publish nằm trong `OutwardService.CreateAsync()` (xem comment ở `Program.cs:152-153`).
- Nếu VoucherWorker publish thêm 1 lần nữa → ledger bị ghi 2 lần → closing bị trừ 2 lần (sai).

→ Luồng tiếp theo: **[Workflow C](#workflow-c--ghi-sổ-cái)**

---

## Workflow C — Ghi sổ cái

```
                Kafka        LedgerWorker       LedgerService      MySQL
                  │                │                    │              │
                  │ consume ledger-│                    │              │
                  │    change      │                    │              │
                  │───────────────►│                    │              │
                  │                │ event_type?        │              │
                  │                ├───────────────────►│              │
                  │                │                    │              │
                  │                │ CREATE + INWARD    │              │
                  │                │ ProcessInwardAsync │              │
                  │                ├───────────────────►│              │
                  │                │                    │ INSERT ledger│
                  │                │                    ├─────────────►│
                  │                │                    │              │
                  │                │                    │ UpsertLedgerDate
                  │                │                    ├─────────────►│
                  │                │                    │              │
                  │                │                    │ UpsertClosing
                  │                │                    │  (delta +qty)│
                  │                │                    ├─────────────►│
                  │                │                    │              │
                  │                │ CREATE + OUTWARD   │              │
                  │                │ ProcessOutwardAsync              │
                  │                ├───────────────────►│              │
                  │                │                    │ INSERT ledger│
                  │                │                    ├─────────────►│
                  │                │                    │              │
                  │                │                    │ UpsertClosing
                  │                │                    │  (delta -qty)│
                  │                │                    ├─────────────►│
                  │                │                    │              │
                  │                │ UPDATE             │              │
                  │                │ ProcessUpdateAsync │              │
                  │                ├───────────────────►│              │
                  │                │ (xem Workflow E)   │              │
```

**Code**:
- Worker: `backend/Workers/LedgerWorker/Program.cs:66-148` (`LedgerKafkaConsumer.HandleMessageAsync`)
- Service: `backend/Workers/LedgerWorker/LedgerService.cs`

**3 method xử lý**:
1. `ProcessInwardAsync` (line 27-53): INSERT ledger entry + UpsertLedgerDate (+qty) + UpsertClosing (+qty)
2. `ProcessOutwardAsync` (line 56-82): INSERT ledger entry + UpsertLedgerDate (-qty) + UpsertClosing (-qty)
3. `ProcessUpdateAsync` (line 108-208): reverse impact cũ + xóa entries cũ + insert entries mới + áp impact mới (xem Workflow E)

**Lưu ý**: `event_type` mặc định là `CREATE` nếu không có trong message (tương thích ngược).

---

## Workflow D — Tạo phiếu nhập thủ công

```
Client        BusinessApi       InwardService       MySQL        Kafka
  │              │                  │                │            │
  │ POST         │                  │                │            │
  │ /inwards     │                  │                │            │
  ├─────────────►│                  │                │            │
  │              │ CreateAsync(dto) │                │            │
  │              ├─────────────────►│                │            │
  │              │                  │ INSERT inward  │            │
  │              │                  ├───────────────►│            │
  │              │                  │                │            │
  │              │                  │ publish ledger-change      │
  │              │                  │ (sync, chờ ack)│            │
  │              │                  ├───────────────────────────►│
  │              │                  │                │            │
  │ 201 Created  │                  │                │            │
  │◄─────────────┤                  │                │            │
```

**Code**:
- Controller: `backend/BusinessApi/Controllers/InwardsController.cs:58-63`
- Service: `InwardService.CreateAsync` (`backend/BE.Application/Services/Inward/InwardService.cs`)

**Khác biệt với Outward**: Inward **chờ Kafka ack đồng bộ** trước khi return 201. Outward fire-and-forget.

→ Sau khi publish: chạy **[Workflow C](#workflow-c--ghi-sổ-cái)** để ghi ledger.

---

## Workflow E — Sửa phiếu nhập/xuất

```
Client        BusinessApi       Inward/Outward    Kafka       LedgerWorker
                │                  Service                      │
  │ PUT         │                  │                │            │
  │ /inwards/id │                  │                │            │
  ├────────────►│                  │                │            │
  │              │ UpdateAsync(dto) │                │            │
  │              ├─────────────────►│                │            │
  │              │                  │ UPDATE bảng   │            │
  │              │                  │                │            │
  │              │                  │ publish ledger-change      │
  │              │                  │ event_type=UPDATE          │
  │              │                  │ + old_quantity,            │
  │              │                  │   old_product_id,          │
  │              │                  │   old_stock_id             │
  │              │                  ├──────────────────────────►│
  │              │                  │                │            │
  │              │                  │                │ consume    │
  │              │                  │                ├───────────►│
  │              │                  │                │            │
  │              │                  │                │ ProcessUpdateAsync
  │              │                  │                ├───────────►│
  │              │                  │                │            │
  │              │                  │                │ 1. Reverse cũ
  │              │                  │                │ 2. Xóa entries cũ
  │              │                  │                │ 3. Insert mới
  │              │                  │                │ 4. Apply impact mới
  │              │                  │                │            │
  │ 200 OK      │                  │                │            │
  │◄─────────────┤                  │                │            │
```

**Code**:
- Inward update: `backend/BE.Application/Services/Inward/InwardService.cs`
- Outward update: `backend/BusinessApi/Controllers/OutwardsController.cs:68-72` — **NHƯNG** phiếu xuất gắn đơn bị từ chối (422) nên Workflow E chủ yếu áp dụng cho **inward** hoặc **outward manual** (không qua đơn)
- LedgerWorker xử lý UPDATE: `LedgerService.ProcessUpdateAsync` (`backend/Workers/LedgerWorker/LedgerService.cs:108-208`)

**Message schema khi UPDATE**:
```json
{
  "voucher_id": "guid",
  "voucher_type": "INWARD",
  "product_id": "guid (mới)",
  "stock_id": "guid (mới)",
  "quantity": 10,
  "timestamp": "...",
  "event_type": "UPDATE",
  "old_quantity": 5,
  "old_product_id": "guid (cũ)",
  "old_stock_id": "guid (cũ)"
}
```

**ProcessUpdateAsync logic**:
1. Lấy tất cả entries cũ theo `reference_id = voucher_id`
2. Với mỗi entry cũ: reverse impact (`closing += outward - inward`, `ledger_date -= inward, -= outward`)
3. Xóa tất cả entries cũ
4. Nếu `quantity mới = 0` → không insert gì (chỉ xóa)
5. Insert entry mới + apply impact mới

---

## Workflow F — Xóa đơn hàng (cascade)

```
Client        OrderApi          OrderService       MySQL        Kafka
  │              │                  │                │            │
  │ DELETE       │                  │                │            │
  │ /orders/id   │                  │                │            │
  ├─────────────►│                  │                │            │
  │              │ RemoveAsync(id)  │                │            │
  │              ├─────────────────►│                │            │
  │              │                  │                │            │
  │              │                  │ SELECT outwards theo order_id
  │              │                  ├───────────────►│            │
  │              │                  │◄───────────────┤            │
  │              │                  │                │            │
  │              │                  │ For each outward:           │
  │              │                  │ publish ledger-change       │
  │              │                  │ event_type=UPDATE           │
  │              │                  │ quantity=0                  │
  │              │                  │ + old_quantity, etc.        │
  │              │                  ├────────────────────────────►│
  │              │                  │                │            │
  │              │                  │ DELETE outwards            │
  │              │                  ├───────────────►│            │
  │              │                  │                │            │
  │              │                  │ DELETE order_items         │
  │              │                  ├───────────────►│            │
  │              │                  │                │            │
  │              │                  │ DELETE order               │
  │              │                  ├───────────────►│            │
  │              │                  │                │            │
  │ 204 No      │                  │                │            │
  │ Content     │                  │                │            │
  │◄─────────────┤                  │                │            │
```

**Code**: `OrderService.RemoveAsync` (`backend/BE.Application/Services/Order/OrderService.cs:307-342`)

**Mục đích**: Khi xóa đơn, outward gắn với đơn không bị xóa ngay (vì Workflow C đang chạy có thể chưa xong). Thay vào đó publish UPDATE với `quantity=0` để LedgerWorker reverse impact cũ, SAU ĐÓ mới xóa outward/order_items/order trong DB.

**LedgerWorker xử lý UPDATE quantity=0**: gọi `ProcessUpdateAsync` với `newQuantity=0` → không insert entry mới, chỉ reverse impact cũ (xem code line 162-167 của LedgerService.cs).

---

## Tổng kết các topic Kafka

| Topic | Producer | Consumer | Trigger | Workflow |
|---|---|---|---|---|
| `order-created` | `OrderApi` | `VoucherWorker` | Tạo đơn | A → B |
| `ledger-change` | `OutwardService` (từ VoucherWorker + Outward manual) | `LedgerWorker` | Sinh outward | B → C |
| `ledger-change` | `InwardService` | `LedgerWorker` | Tạo phiếu nhập | D → C |
| `ledger-change` | `InwardService`/`OutwardService` | `LedgerWorker` | Sửa phiếu | E → C |
| `ledger-change` | `OrderService` | `LedgerWorker` | Xóa đơn (cascade) | F → C |

Xem thêm chi tiết về message schema: [../02-architecture/communication.md](../02-architecture/communication.md).