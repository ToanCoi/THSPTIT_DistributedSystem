using System;

namespace BE.HostBase.Interfaces
{
    /// <summary>
    /// Interface service xử lý JWT token
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Lấy user_id từ JWT token trong request hiện tại
        /// </summary>
        /// <returns>User ID hoặc null nếu không có token</returns>
        Guid? GetCurrentUserId();

        /// <summary>
        /// Lấy username từ JWT token
        /// </summary>
        /// <returns>Username hoặc null</returns>
        string? GetCurrentUsername();

        /// <summary>
        /// Lấy role từ JWT token
        /// </summary>
        /// <returns>Role code hoặc null</returns>
        string? GetCurrentRole();

        /// <summary>
        /// Kiểm tra token có hợp lệ không
        /// </summary>
        bool IsTokenValid();
    }
}