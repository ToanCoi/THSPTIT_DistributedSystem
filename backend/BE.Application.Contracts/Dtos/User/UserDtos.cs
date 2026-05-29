using System;

namespace BE.Application.Contracts.Dtos.User
{
    /// <summary>
    /// DTO đăng nhập
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// Tên đăng nhập
        /// </summary>
        public string username { get; set; }

        /// <summary>
        /// Mật khẩu (plain text)
        /// </summary>
        public string password { get; set; }
    }

    /// <summary>
    /// DTO đăng ký tài khoản
    /// </summary>
    public class RegisterDto
    {
        /// <summary>
        /// Tên đăng nhập
        /// </summary>
        public string username { get; set; }

        /// <summary>
        /// Mật khẩu (plain text, sẽ được hash)
        /// </summary>
        public string password { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// Họ và tên đầy đủ
        /// </summary>
        public string full_name { get; set; }

        /// <summary>
        /// Số điện thoại (optional)
        /// </summary>
        public string? phone_number { get; set; }
    }

    /// <summary>
    /// DTO refresh token
    /// </summary>
    public class RefreshTokenDto
    {
        /// <summary>
        /// Refresh token
        /// </summary>
        public string refresh_token { get; set; }
    }

    /// <summary>
    /// DTO thông tin user trả về
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// ID người dùng
        /// </summary>
        public Guid user_id { get; set; }

        /// <summary>
        /// Tên đăng nhập
        /// </summary>
        public string username { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string email { get; set; }

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
        /// Trạng thái hoạt động
        /// </summary>
        public bool is_active { get; set; }

        /// <summary>
        /// Vai trò
        /// </summary>
        public string role_code { get; set; }
    }

    /// <summary>
    /// DTO phản hồi đăng nhập (chứa JWT token)
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>
        /// JWT Access Token
        /// </summary>
        public string access_token { get; set; }

        /// <summary>
        /// Loại token (Bearer)
        /// </summary>
        public string token_type { get; set; } = "Bearer";

        /// <summary>
        /// Thời hạn token (giây)
        /// </summary>
        public int expires_in { get; set; }

        /// <summary>
        /// Refresh token
        /// </summary>
        public string refresh_token { get; set; }

        /// <summary>
        /// Thông tin user
        /// </summary>
        public UserDto user { get; set; }
    }

    /// <summary>
    /// DTO đổi mật khẩu
    /// </summary>
    public class ChangePasswordDto
    {
        /// <summary>
        /// Mật khẩu hiện tại
        /// </summary>
        public string current_password { get; set; }

        /// <summary>
        /// Mật khẩu mới
        /// </summary>
        public string new_password { get; set; }
    }

    /// <summary>
    /// DTO đăng nhập Google
    /// </summary>
    public class GoogleLoginDto
    {
        /// <summary>
        /// Google ID token từ client
        /// </summary>
        public string google_token { get; set; }

        /// <summary>
        /// Email từ Google
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// Họ và tên từ Google
        /// </summary>
        public string full_name { get; set; }
    }
}