using BE.Domain.Share.Entities;
using System;

namespace BE.Domain.Entities
{
    /// <summary>
    /// Entity người dùng hệ thống
    /// </summary>
    public class UserEntity : BaseEntity
    {
        /// <summary>
        /// ID người dùng (GUID)
        /// </summary>
        public Guid user_id { get; set; }

        /// <summary>
        /// Tên đăng nhập
        /// </summary>
        public string username { get; set; }

        /// <summary>
        /// Mật khẩu đã hash (bcrypt)
        /// </summary>
        public string password_hash { get; set; }

        /// <summary>
        /// Email đăng nhập
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// Google ID cho OAuth
        /// </summary>
        public string google_id { get; set; }

        /// <summary>
        /// Họ và tên đầy đủ
        /// </summary>
        public string full_name { get; set; }

        /// <summary>
        /// Số điện thoại
        /// </summary>
        public string phone_number { get; set; }

        /// <summary>
        /// URL avatar
        /// </summary>
        public string avatar_url { get; set; }

        /// <summary>
        /// Trạng thái hoạt động: 1 = hoạt động, 0 = khóa
        /// </summary>
        public bool is_active { get; set; }

        /// <summary>
        /// Đã xác thực email chưa
        /// </summary>
        public bool is_verified { get; set; }

        /// <summary>
        /// JWT Refresh Token
        /// </summary>
        public string refresh_token { get; set; }

        /// <summary>
        /// Thời hạn refresh token
        /// </summary>
        public DateTime? refresh_token_expire { get; set; }

        /// <summary>
        /// Mã vai trò: ADMIN, USER, MANAGER
        /// </summary>
        public string role_code { get; set; }

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