# PRD: Tách biệt luồng sinh phiếu kho và ghi sổ tồn kho

## Problem Statement

Hiện tại hệ thống ghi sổ tồn kho (ledger) TRỰC TIẾP từ đơn hàng thông qua Kafka message `order-created` → HandleWorker. Điều này vi phạm nghiệp vụ thực tế: **sổ tồn kho phải được ghi từ phiếu xuất kho / nhập kho**, không phải trực tiếp từ đơn hàng.

Ngoài ra, HandleWorker vừa sinh phiếu xuất kho vừa ghi ledger — chưa tách biệt trách nhiệm.

## Solution

Tách thành 3 thành phần rõ ràng:

1. **VoucherWorker** — lắng nghe `order-created`, sinh phiếu xuất kho vào DB, rồi push sang `ledger-change`
2. **LedgerWorker** (rename từ HandleWorker) — lắng nghe `ledger-change`, ghi sổ tồn kho từ phiếu nhập/xuất kho
3. **Business API** — khi tạo phiếu nhập kho hoặc phiếu xuất kho thủ công, tự push `ledger-change`

## User Stories

1. As a **VoucherWorker**, I want to listen to `order-created` topic, so that I can create outward voucher records from orders
2. As a **VoucherWorker**, I want to push `ledger-change` message after creating outward voucher, so that LedgerWorker can update the inventory ledger
3. As a **LedgerWorker**, I want to listen to `ledger-change` topic, so that I can record inventory movements from both inward and outward vouchers
4. As a **InwardsController**, I want to push `ledger-change` after creating an inward voucher, so that the inventory ledger is updated when goods are received
5. As a **OutwardsController**, I want to push `ledger-change` after creating an outward voucher (manual export), so that the inventory ledger is updated for non-order exports
6. As an **OrderService**, I want to include `stock_id` in the order creation DTO, so that the system knows which warehouse the order draws from
7. As a **system operator**, I want the ledger to be updated ONLY from vouchers (inward/outward), so that the inventory ledger accurately reflects actual stock movements
8. As a **VoucherWorker**, I want to reuse `OutwardService.Create()` for creating outward vouchers, so that the creation logic is centralized and not duplicated
9. As a **VoucherWorker**, I want to push `ledger-change` synchronously (wait for Kafka ack), so that the ledger update is triggered reliably
10. As a **system**, if `ledger-change` push fails after outward voucher is created, I want to NOT rollback the voucher record, so that a later sync job can reconcile the discrepancy

## Implementation Decisions

### 1. Cấu trúc thư mục Workers

```
backend/Workers/
├── Workers.Shared/           # Shared library cho cả LedgerWorker và VoucherWorker
│   ├── Models/
│   │   ├── OrderCreatedMessage.cs
│   │   └── LedgerChangeMessage.cs
│   └── Services/
│       └── KafkaConsumerBase.cs
├── LedgerWorker/             # Rename từ HandleWorker
│   ├── Program.cs
│   ├── LedgerService.cs
│   └── Dockerfile
└── VoucherWorker/            # Worker mới
    ├── Program.cs
    ├── VoucherProcessingService.cs
    └── Dockerfile
```

### 2. Message schemas

**`order-created` (existing, consumed by VoucherWorker):**
```json
{
  "order_id": "uuid",
  "customer_id": "uuid",
  "stock_id": "uuid",          // Thêm mới — kho xuất
  "order_code": "string",
  "items": [
    {
      "product_id": "uuid",
      "quantity": 10,
      "unit_price": 100
    }
  ],
  "timestamp": "ISO8601"
}
```

**`ledger-change` (new, produced by VoucherWorker & Business API, consumed by LedgerWorker):**
```json
{
  "voucher_id": "uuid",
  "voucher_type": "INWARD | OUTWARD",
  "product_id": "uuid",
  "stock_id": "uuid",
  "quantity": 10,
  "timestamp": "ISO8601"
}
```

### 3. Kafka Topics

| Topic | Producer | Consumer | Purpose |
|-------|----------|----------|---------|
| `order-created` | OrderApi | VoucherWorker | Trigger voucher creation from order |
| `ledger-change` | VoucherWorker, InwardsController, OutwardsController | LedgerWorker | Trigger ledger write from voucher |

### 4. Thay đổi Interface

**`ILedgerService`**:
- `ProcessInwardAsync(inwardId, productId, stockId, quantity)` — bỏ `unitPrice`
- `ProcessOutwardAsync(outwardId, productId, stockId, quantity)` — bỏ `unitPrice`
- XÓA `ProcessOrderItemAsync` (không còn dùng trực tiếp)

### 5. Thay đổi DTO

**`OrderCreateDto`** — thêm field:
```csharp
public Guid stock_id { get; set; }
```

**`OrderItemCreateDto`** — giữ nguyên (đã có product_id, quantity, unit_price)

### 6. OutwardService.Create()

- KHÔNG push Kafka
- Trả về `OutwardDto` chứa `outward_id`, `order_id`, `product_id`, `stock_id`, `quantity`, `unit_price`
- Caller (VoucherWorker hoặc OutwardsController) chịu trách nhiệm push `ledger-change`

### 7. VoucherWorker flow

1. Consume `order-created`
2. Với mỗi item trong order:
   a. Gọi `OutwardService.CreateAsync(OutwardCreateDto)` với `order_id`, `product_id`, `stock_id`, `quantity`, `unit_price`
   b. Sau khi ghi outward thành công → produce `ledger-change` message lên Kafka `ledger-change` topic
3. Nếu step 2b thất bại: log lỗi, KHÔNG rollback outward đã ghi

### 8. LedgerWorker flow

1. Consume `ledger-change` từ topic `ledger-change`
2. Deserialize, check `voucher_type`:
   - `"INWARD"` → gọi `ProcessInwardAsync`
   - `"OUTWARD"` → gọi `ProcessOutwardAsync`
3. `ProcessOutwardAsync` và `ProcessInwardAsync` giữ nguyên logic hiện tại (ghi `led_inventory_item_ledger`, `led_inventory_item_ledger_date`, `led_inventory_item_ledger_closing`)

### 9. InwardsController flow (sửa đổi)

1. `Create()` → gọi `InwardService.CreateAsync(dto)` → ghi bảng `inwards`
2. Sau khi ghi thành công → produce `ledger-change` (voucher_type=`INWARD`) lên Kafka `ledger-change`
3. Chờ Kafka ack đồng bộ rồi mới return HTTP 201

### 10. OutwardsController flow (sửa đổi)

1. `Create()` → gọi `OutwardService.CreateAsync(dto)` → ghi bảng `outwards`
2. Sau khi ghi thành công → produce `ledger-change` (voucher_type=`OUTWARD`) lên Kafka `ledger-change`
3. Fire-and-forget (không cần chờ ack)

### 11. docker-compose.yml thay đổi

- `handle-worker` container → `ledger-worker`
- Thêm `voucher-worker` service mới
- Thêm Kafka topic `ledger-change`
- Env `Kafka__LedgerTopic=ledger-change`

### 12. Workers.Shared library

- **`OrderCreatedMessage`**: deserializable model cho message `order-created` (đã có từ HandleWorker, chuyển sang Shared)
- **`LedgerChangeMessage`**: model cho message `ledger-change`
- **`KafkaConsumerBase`**: abstract base class cho Kafka consumer, xử lý connection, subscribe, consume loop — LedgerWorker và VoucherWorker kế thừa

## Testing Decisions

- **VoucherWorker**: test với mock Kafka — verify `OutwardService.Create()` được gọi đúng số lần (bằng số items trong order) và `ledger-change` message được produce đúng format
- **LedgerWorker**: test với mock Kafka — verify đúng `ProcessInwardAsync`/`ProcessOutwardAsync` được gọi theo `voucher_type`
- **InwardsController / OutwardsController**: integration test verify Kafka message được produce sau khi tạo voucher
- Chỉ test behavior bên ngoài (Kafka message output, DB records) — không test internal implementation

## Out of Scope

- Thay đổi luồng xuất kho cho đơn hàng bị hủy / hoàn tiền
- Tính closing balance chính xác (hiện tại đang overwrite, cần tính lại tổng nhập - tổng xuất)
- Unit test cho SQL trong LedgerRepo
- UI thay đổi cho việc chọn kho trên đơn hàng
