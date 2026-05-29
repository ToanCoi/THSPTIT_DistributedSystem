using System;

namespace BE.Domain.Entities
{
    /// <summary>
    /// Entity kho hàng
    /// </summary>
    public class StockEntity
    {
        /// <summary>
        /// ID kho
        /// </summary>
        public Guid stock_id { get; set; }

        /// <summary>
        /// Mã kho
        /// </summary>
        public string stock_code { get; set; }

        /// <summary>
        /// Tên kho
        /// </summary>
        public string stock_name { get; set; }

        /// <summary>
        /// Địa chỉ kho
        /// </summary>
        public string address { get; set; }

        /// <summary>
        /// Ngày tạo
        /// </summary>
        public DateTime created_date { get; set; }

        /// <summary>
        /// Người tạo
        /// </summary>
        public string created_by { get; set; }

        /// <summary>
        /// Ngày sửa
        /// </summary>
        public DateTime? modified_date { get; set; }

        /// <summary>
        /// Người sửa
        /// </summary>
        public string modified_by { get; set; }
    }
}