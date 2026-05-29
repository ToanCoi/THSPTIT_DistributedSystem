using System;

namespace BE.Domain.Entities
{
    /// <summary>
    /// Entity phiếu nhập kho
    /// </summary>
    public class InwardEntity
    {
        /// <summary>
        /// ID phiếu nhập
        /// </summary>
        public Guid inward_id { get; set; }

        /// <summary>
        /// ID sản phẩm
        /// </summary>
        public Guid product_id { get; set; }

        /// <summary>
        /// ID kho
        /// </summary>
        public Guid stock_id { get; set; }

        /// <summary>
        /// Số lượng nhập
        /// </summary>
        public decimal quantity { get; set; }

        /// <summary>
        /// Đơn giá
        /// </summary>
        public decimal unit_price { get; set; }

        /// <summary>
        /// Nhà cung cấp
        /// </summary>
        public string supplier { get; set; }

        /// <summary>
        /// Ngày hóa đơn
        /// </summary>
        public DateTime invoice_date { get; set; }

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