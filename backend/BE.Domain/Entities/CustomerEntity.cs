using System;

namespace BE.Domain.Entities
{
    /// <summary>
    /// Entity khách hàng
    /// </summary>
    public class CustomerEntity
    {
        /// <summary>
        /// ID khách hàng
        /// </summary>
        public Guid customer_id { get; set; }

        /// <summary>
        /// ID người dùng (liên kết với auth)
        /// </summary>
        public Guid? user_id { get; set; }

        /// <summary>
        /// Họ và tên đầy đủ
        /// </summary>
        public string full_name { get; set; }

        /// <summary>
        /// Số điện thoại
        /// </summary>
        public string phone { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// Địa chỉ
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