# Tổng quan nghiệp vụ

## 1. Bài toán

Xây dựng hệ thống bán hàng B2C (Business-to-Customer) cho phép:

- Quản lý danh mục **khách hàng**, **sản phẩm**, **kho hàng**
- Tạo và quản lý **đơn hàng** với nhiều sản phẩm
- Quản lý **phiếu nhập kho** (inward) và **phiếu xuất kho** (outward)
- Tự động cập nhật **sổ tồn kho** (ledger) dựa trên phiếu nhập/xuất
- Xem lịch sử biến động kho theo ngày và tồn cuối kỳ theo sản phẩm

Hệ thống phục vụ mục tiêu học thuật: minh họa kiến trúc microservice với event-driven flow qua Kafka.

## 2. Phạm vi

### Trong phạm vi

- CRUD đầy đủ cho **Customer**, **Product**, **Stock**
- CRUD đầy đủ cho **Inward**, **Outward**, **Order**
- Quản lý giá bán theo sản phẩm (`ProductPrices`)
- Sổ tồn kho với 3 bảng: `led_inventory_item_ledger`, `led_inventory_item_ledger_date`, `led_inventory_item_ledger_closing`
- Authentication với JWT (login, register, Google login, refresh token)
- Phân quyền đơn giản theo role (`USER`, `ADMIN`)
- Logging tập trung qua ELK (khi chạy trên k8s)

### Ngoài phạm vi (chưa làm)

- **Thanh toán online**: đơn hàng chỉ ghi nhận, không qua cổng thanh toán
- **Multi-tenant**: hiện tại single-tenant, 1 database `business_db` dùng chung
- **Hủy đơn/hoàn tiền**: chỉ có thao tác xóa đơn (cascade), không có workflow hủy chuẩn
- **Báo cáo doanh thu**: chưa có dashboard phân tích
- **Tính closing chính xác bằng tổng nhập - tổng xuất**: hiện tại đang cộng dồn delta, có thể lệch nếu có sửa phiếu nhiều lần (xem `LedgerService.ProcessUpdateAsync`)
- **Rate limiting, circuit breaker, retry policy**: chưa có

## 3. Actors

| Actor | Mô tả | Quyền |
|---|---|---|
| **Khách hàng (Customer)** | Người mua hàng. Tạo đơn, xem đơn của mình. | (Trong demo, đăng nhập chung, không phân biệt user/khách hàng) |
| **Nhân viên kho (Stock clerk)** | Tạo phiếu nhập/xuất, xem tồn kho. | Tất cả quyền |
| **Admin** | Quản lý user, cấu hình hệ thống. | Tất cả quyền + quản lý `users` |

Trong demo, tất cả user đều dùng chung giao diện và có quyền giống nhau; phân quyền chỉ phân biệt qua trường `role_code` trong bảng `users` (`USER` hoặc `ADMIN`).

## 4. Use cases chính

### Quản lý danh mục

| Use case | Endpoint | Mô tả |
|---|---|---|
| CRUD Khách hàng | `GET/POST/PUT/DELETE /Business/api/customers` | Thêm/sửa/xóa khách hàng |
| CRUD Sản phẩm | `GET/POST/PUT/DELETE /Business/api/products` | Thêm/sửa/xóa sản phẩm |
| CRUD Kho | `GET/POST/PUT/DELETE /Business/api/stocks` | Thêm/sửa/xóa kho |
| Giá bán theo SP | `GET /Business/api/productprices/{id}/selling-price` | Xem giá bán |
| Tồn theo kho | `GET /Business/api/productprices/{id}/stock/{stockId}` | Xem tồn kho của 1 sản phẩm ở 1 kho |

### Quản lý kho

| Use case | Endpoint | Mô tả |
|---|---|---|
| CRUD Phiếu nhập | `GET/POST/PUT/DELETE /Business/api/inwards` | Tạo phiếu nhập → publish `ledger-change` |
| CRUD Phiếu xuất | `GET/POST/PUT/DELETE /Business/api/outwards` | Tạo phiếu xuất (manual, không qua đơn) → publish `ledger-change` |

### Quản lý đơn hàng

| Use case | Endpoint | Mô tả |
|---|---|---|
| CRUD Đơn hàng | `GET/POST/PUT/DELETE /Order/api/orders` | Tạo đơn → publish `order-created` → VoucherWorker sinh outward → publish `ledger-change` |
| Cập nhật trạng thái | `PUT /Order/api/orders/{id}/status` | Đổi status (PENDING, CONFIRMED, ...) |
| Lấy đơn theo ID | `GET /Order/api/orders/{id}` | Lấy chi tiết + items |

### Xác thực

| Use case | Endpoint | Mô tả |
|---|---|---|
| Đăng nhập | `POST /Auth/login` | Username/password → JWT |
| Đăng ký | `POST /Auth/register` | Tạo user mới |
| Google login | `POST /Auth/google` | OAuth Google |
| Refresh token | `POST /Auth/refresh-token` | Lấy access token mới |
| Logout | `POST /Auth/logout` | Revoke refresh token |
| Đổi mật khẩu | `POST /Auth/change-password` | Đổi password (cần auth) |
| Thông tin cá nhân | `GET /Auth/me` | Lấy user hiện tại (cần auth) |

## 5. Nguyên tắc nghiệp vụ cốt lõi

### 5.1 Sổ tồn kho ghi nhận từ phiếu, không ghi trực tiếp từ đơn

Đây là **nguyên tắc quan trọng nhất** của hệ thống (PRD cũ đã được hợp nhất vào tài liệu `workflows.md`).

**Trước** (thiết kế ban đầu, đã bỏ):
```
Order → publish order-created → HandleWorker ghi ledger trực tiếp
```
Vấn đề: ledger ghi nhận từ đơn hàng, không phản ánh đúng biến động kho thực tế.

**Sau** (thiết kế hiện tại):
```
Order → publish order-created → VoucherWorker sinh Outward → publish ledger-change → LedgerWorker ghi ledger
                                     ↑                                                  ↑
                                     │                                                  │
                              (phiếu xuất)                                  (sổ tồn kho)
```
Mọi thay đổi tồn kho đều đi qua **phiếu nhập/xuất** trước khi vào sổ cái.

### 5.2 Cascade khi xóa đơn

Khi xóa đơn hàng, hệ thống phải:
1. Publish `ledger-change event_type=UPDATE quantity=0` cho **từng outward** liên quan đến đơn đó, kèm `old_quantity`, `old_product_id`, `old_stock_id` để LedgerWorker reverse impact cũ
2. Xóa outwards + order_items + order

Code: `OrderService.RemoveAsync()` (`backend/BE.Application/Services/Order/OrderService.cs:307-342`).

### 5.3 Sửa phiếu nhập/xuất

Khi sửa inward/outward, hệ thống publish `ledger-change event_type=UPDATE` kèm:
- Thông tin mới: `product_id`, `stock_id`, `quantity`
- Thông tin cũ: `old_product_id`, `old_stock_id`, `old_quantity`

LedgerWorker sẽ:
1. Reverse impact cũ (trừ closing, trừ ledger_date)
2. Xóa entries cũ
3. Insert entries mới + apply impact mới

Code: `LedgerService.ProcessUpdateAsync()` (`backend/Workers/LedgerWorker/LedgerService.cs:108-208`).

### 5.4 Số lượng tồn kho (closing)

`led_inventory_item_ledger_closing.quantity` được cập nhật theo **delta**:
- Nhập kho: `closing += quantity`
- Xuất kho: `closing -= quantity`
- Sửa phiếu: reverse impact cũ, áp impact mới

Hiện tại chưa có cơ chế "tính lại từ đầu" (rebuild tổng). Xem [Out of scope](#2-phạm-vi).

### 5.5 Trạng thái đơn hàng

Đơn hàng có field `status` (string, không enum). Giá trị thường gặp:
- `PENDING` (mặc định khi tạo)
- `CONFIRMED` (sau khi xác nhận)
- (Chưa có workflow chuyển trạng thái chuẩn — chỉ cập nhật qua API)

### 5.6 Sinh mã đơn tự động

Mã đơn format `DH{sequence}`, sequence tăng dần, atomic với row lock trong MySQL (`SELECT ... FOR UPDATE`).
Code: `OrderService.GenerateOrderCodeAsync()` (`backend/BE.Application/Services/Order/OrderService.cs:276-280`).

### 5.7 Ràng buộc phiếu xuất

- Phiếu xuất gắn với đơn hàng (`order_id IS NOT NULL`) **không được sửa/xóa** (trả 422). Chỉ phiếu xuất manual (tạo trực tiếp từ `/Business/api/outwards`) mới có thể sửa/xóa.
- Lý do: nếu sửa phiếu xuất từ đơn, phải tính lại closing của cả đơn — phức tạp. Code: `OutwardService.UpdateAsync`/`RemoveAsync` (`backend/BE.Application/Services/Outward/OutwardService.cs`).

## 6. Tài liệu liên quan

- Chi tiết luồng: [workflows.md](workflows.md)
- Chi tiết dữ liệu: [data-model.md](data-model.md)
- Kiến trúc: [../02-architecture/](../02-architecture/README.md)
- Triển khai: [../03-deployment/](../03-deployment/README.md)