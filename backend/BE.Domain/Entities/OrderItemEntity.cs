using System;

namespace BE.Domain.Entities
{
    /// <summary>
    /// Entity chi tiết đơn hàng
    /// </summary>
    public class OrderItemEntity
    {
        /// <summary>
        /// ID chi tiết đơn hàng
        /// </summary>
        public Guid order_item_id { get; set; }

        /// <summary>
        /// ID đơn hàng
        /// </summary>
        public Guid order_id { get; set; }

        /// <summary>
        /// ID sản phẩm
        /// </summary>
        public Guid product_id { get; set; }

        /// <summary>
        /// Số lượng
        /// </summary>
        public decimal quantity { get; set; }

        /// <summary>
        /// Đơn giá
        /// </summary>
        public decimal unit_price { get; set; }

        /// <summary>
        /// Ngày tạo
        /// </summary>
        public DateTime created_date { get; set; }

        /// <summary>
        /// Người tạo
        /// </summary>
        public string created_by { get; set; }
    }
}