using System;

namespace BE.Domain.Entities
{
    /// <summary>
    /// Entity sản phẩm
    /// </summary>
    public class ProductEntity
    {
        /// <summary>
        /// ID sản phẩm
        /// </summary>
        public Guid product_id { get; set; }

        /// <summary>
        /// Mã sản phẩm
        /// </summary>
        public string product_code { get; set; }

        /// <summary>
        /// Tên sản phẩm
        /// </summary>
        public string product_name { get; set; }

        /// <summary>
        /// Giá bán
        /// </summary>
        public decimal price { get; set; }

        /// <summary>
        /// Đơn vị tính
        /// </summary>
        public string unit { get; set; }

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