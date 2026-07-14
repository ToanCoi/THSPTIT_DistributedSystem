using System;

namespace BE.Domain.Entities
{
    /// <summary>
    /// Entity phiếu xuất kho
    /// </summary>
    public class OutwardEntity
    {
        /// <summary>
        /// ID phiếu xuất
        /// </summary>
        public Guid outward_id { get; set; }

        /// <summary>
        /// ID đơn hàng (nếu có)
        /// </summary>
        public Guid? order_id { get; set; }

        /// <summary>
        /// ID sản phẩm
        /// </summary>
        public Guid product_id { get; set; }

        /// <summary>
        /// ID kho
        /// </summary>
        public Guid stock_id { get; set; }

        /// <summary>
        /// Số lượng xuất
        /// </summary>
        public decimal quantity { get; set; }

        /// <summary>
        /// Đơn giá
        /// </summary>
        public decimal unit_price { get; set; }

        /// <summary>
        /// Ngày xuất kho
        /// </summary>
        public DateTime outward_date { get; set; }

        /// <summary>
        /// Ngày tạo
        /// </summary>
        public DateTime created_date { get; set; }

        /// <summary>
        /// Người tạo
        /// </summary>
        public string created_by { get; set; }

        /// <summary>
        /// Mã đơn hàng (JOIN từ bảng orders, dùng cho hiển thị)
        /// </summary>
        public string? order_code { get; set; }

        /// <summary>
        /// Tên sản phẩm (JOIN từ bảng products, dùng cho hiển thị)
        /// </summary>
        public string? product_name { get; set; }

        /// <summary>
        /// Tên kho (JOIN từ bảng stocks, dùng cho hiển thị)
        /// </summary>
        public string? stock_name { get; set; }
    }
}