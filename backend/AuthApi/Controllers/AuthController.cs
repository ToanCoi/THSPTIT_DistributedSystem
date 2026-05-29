using BE.Application.Contracts.Dtos.User;
using BE.Application.Contracts.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AuthApi.Controllers
{
    /// <summary>
    /// Controller xác thực và quản lý tài khoản người dùng
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// Khởi tạo AuthController
        /// </summary>
        /// <param name="authService">Service xác thực</param>
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Đăng nhập người dùng
        /// </summary>
        /// <param name="loginDto">Thông tin đăng nhập</param>
        /// <returns>Token và thông tin user</returns>
        [HttpPost("login")]
        [AllowAnonymous] 
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            return Ok(result);
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        /// <param name="registerDto">Thông tin đăng ký</param>
        /// <returns>Thông tin user đã tạo</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            return Created($"/Auth/users/{result.user_id}", result);
        }

        /// <summary>
        /// Đăng nhập Google
        /// </summary>
        /// <param name="googleDto">Thông tin Google token</param>
        /// <returns>Token và thông tin user</returns>
        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto googleDto)
        {
            var result = await _authService.GoogleLoginAsync(googleDto);
            return Ok(result);
        }

        /// <summary>
        /// Làm mới access token bằng refresh token
        /// </summary>
        /// <param name="refreshTokenDto">Refresh token</param>
        /// <returns>Token mới</returns>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            var result = await _authService.RefreshTokenAsync(refreshTokenDto);
            return Ok(result);
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = GetCurrentUserId();
            await _authService.LogoutAsync(userId);
            return NoContent();
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <param name="changePasswordDto">Mật khẩu cũ và mới</param>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var userId = GetCurrentUserId();
            await _authService.ChangePasswordAsync(userId, changePasswordDto);
            return NoContent();
        }

        /// <summary>
        /// Lấy thông tin user hiện tại
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            var result = await _authService.GetCurrentUserAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy user ID từ JWT token
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                              ?? User.FindFirst("sub");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid token");
            }

            return userId;
        }
    }
}