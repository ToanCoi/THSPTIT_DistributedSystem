# 01 — Nghiệp vụ

Phần này mô tả **nghiệp vụ** của hệ thống, đọc từ tổng quan đến chi tiết.

| File | Nội dung |
|---|---|
| [overview.md](overview.md) | Bài toán, phạm vi, actors, use cases tổng quan |
| [workflows.md](workflows.md) | Các luồng nghiệp vụ chính (sequence diagram) |
| [data-model.md](data-model.md) | Entities, ERD, schema MySQL, ràng buộc |

## Glossary

| Thuật ngữ | Tiếng Việt | Giải thích |
|---|---|---|
| Customer | Khách hàng | Người mua hàng. Mỗi khách hàng có `customer_id`. |
| Product | Sản phẩm | Hàng hóa bán. Có `product_code`, `product_name`, `price`, `unit`. |
| Stock | Kho | Địa điểm lưu trữ hàng. Mỗi kho có `stock_code`, `stock_name`. |
| Order | Đơn hàng | Yêu cầu mua hàng của khách. Có nhiều `order_item`. |
| Order Item | Chi tiết đơn hàng | 1 dòng sản phẩm trong đơn: `product_id`, `quantity`, `unit_price`. |
| Inward voucher (Inward) | Phiếu nhập kho | Ghi nhận hàng vào kho. Cộng (+) tồn kho. |
| Outward voucher (Outward) | Phiếu xuất kho | Ghi nhận hàng ra khỏi kho. Trừ (-) tồn kho. |
| Ledger entry | Bút toán sổ cái | 1 dòng trong sổ cái tồn kho. Mỗi phiếu nhập/xuất sinh ≥1 entry. |
| Ledger date | Tổng hợp theo ngày | Tổng nhập/xuất theo `(product_id, stock_id, date)`. |
| Closing balance | Tồn cuối kỳ | Tổng tồn hiện tại theo `(product_id, stock_id)`. |
| Order code | Mã đơn hàng | Format `DH{sequence}`, ví dụ `DH1`, `DH2`, `DH3`. |
| Workflow A-F | Luồng A-F | 6 luồng nghiệp vụ chính — xem [workflows.md](workflows.md). |

## Quy ước đọc

- **Tổng quan**: đọc `overview.md` trước
- **Chi tiết luồng**: đọc `workflows.md` để hiểu cách dữ liệu chảy qua hệ thống
- **Chi tiết dữ liệu**: đọc `data-model.md` khi cần biết schema cụ thể