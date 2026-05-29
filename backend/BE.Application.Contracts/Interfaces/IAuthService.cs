using BE.Application.Contracts.Dtos.User;
using System.Threading.Tasks;

namespace BE.Application.Contracts.Interfaces
{
    /// <summary>
    /// Interface service xác thực và quản lý người dùng
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Đăng nhập người dùng
        /// </summary>
        /// <param name="loginDto">Thông tin đăng nhập</param>
        /// <returns>Token và thông tin user</returns>
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        /// <param name="registerDto">Thông tin đăng ký</param>
        /// <returns>Thông tin user đã tạo</returns>
        Task<UserDto> RegisterAsync(RegisterDto registerDto);

        /// <summary>
        /// Làm mới access token bằng refresh token
        /// </summary>
        /// <param name="refreshTokenDto">Refresh token</param>
        /// <returns>Token mới</returns>
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);

        /// <summary>
        /// Đăng xuất (invalidate refresh token)
        /// </summary>
        /// <param name="userId">ID user cần đăng xuất</param>
        Task LogoutAsync(System.Guid userId);

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <param name="userId">ID user</param>
        /// <param name="changePasswordDto">Mật khẩu cũ và mới</param>
        Task ChangePasswordAsync(System.Guid userId, ChangePasswordDto changePasswordDto);

        /// <summary>
        /// Lấy thông tin user hiện tại
        /// </summary>
        /// <param name="userId">ID user</param>
        /// <returns>Thông tin user</returns>
        Task<UserDto> GetCurrentUserAsync(System.Guid userId);

        /// <summary>
        /// Đăng nhập bằng Google OAuth
        /// </summary>
        /// <param name="googleDto">Thông tin đăng nhập Google</param>
        /// <returns>Token và thông tin user</returns>
        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto googleDto);
    }
}