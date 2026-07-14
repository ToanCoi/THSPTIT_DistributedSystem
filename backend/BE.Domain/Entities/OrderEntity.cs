using System;

namespace BE.Domain.Entities
{
    /// <summary>
    /// Entity đơn hàng
    /// </summary>
    public class OrderEntity
    {
        /// <summary>
        /// ID đơn hàng
        /// </summary>
        public Guid order_id { get; set; }

        /// <summary>
        /// ID khách hàng
        /// </summary>
        public Guid customer_id { get; set; }

        /// <summary>
        /// Mã đơn hàng
        /// </summary>
        public string order_code { get; set; }

        /// <summary>
        /// Tổng tiền
        /// </summary>
        public decimal total_amount { get; set; }

        /// <summary>
        /// Trạng thái đơn hàng
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// Ngày đặt hàng
        /// </summary>
        public DateTime order_date { get; set; }

        /// <summary>
        /// Ngày tạo
        /// </summary>
        public DateTime created_date { get; set; }

        /// <summary>
        /// Người tạo
        /// </summary>
        public string created_by { get; set; }

        /// <summary>
        /// ID kho (FK tới stocks)
        /// </summary>
        public Guid stock_id { get; set; }

        /// <summary>
        /// Tên khách hàng (JOIN từ bảng customers, dùng cho hiển thị)
        /// </summary>
        public string? customer_name { get; set; }

        /// <summary>
        /// Tên kho (JOIN từ bảng stocks, dùng cho hiển thị)
        /// </summary>
        public string? stock_name { get; set; }
    }
}