using BE.Application.Contracts.Dtos.User;
using BE.Application.Contracts.Interfaces;
using BE.Application.Exceptions;
using BE.Domain.DI.User;
using BE.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Service xử lý đăng nhập, đăng ký, JWT token
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepo _userRepo;
        private readonly ILogger<AuthService> _logger;

        // JWT Configuration - nên đặt trong config
        private const string SECRET_KEY = "Ecom_Microservice_SecretKey_MustBeAtLeast32Characters!2026";
        private const string ISSUER = "Ecom.AuthApi";
        private const string AUDIENCE = "Ecom.Client";
        private const int ACCESS_TOKEN_EXPIRE_MINUTES = 60;
        private const int REFRESH_TOKEN_EXPIRE_DAYS = 7;

        public AuthService(IUserRepo userRepo, ILogger<AuthService> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Lấy user theo username
            var user = await _userRepo.GetByUsernameAsync(loginDto.username);
            if (user == null)
            {
                _logger.LogWarning("Đăng nhập thất bại: username không tồn tại [{username}]", loginDto.username);
                throw new BusinessException("Tên đăng nhập hoặc mật khẩu không đúng", 401);
            }

            // Kiểm tra trạng thái hoạt động
            if (!user.is_active)
            {
                _logger.LogWarning("Đăng nhập thất bại: tài khoản bị khóa [{username}]", loginDto.username);
                throw new BusinessException("Tài khoản đã bị khóa", 401);
            }

            // Kiểm tra mật khẩu
            if (!BCrypt.Net.BCrypt.Verify(loginDto.password, user.password_hash))
            {
                _logger.LogWarning("Đăng nhập thất bại: sai mật khẩu [{username}]", loginDto.username);
                throw new BusinessException("Tên đăng nhập hoặc mật khẩu không đúng", 401);
            }

            // Tạo tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpire = DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRE_DAYS);

            // Lưu refresh token vào DB
            await _userRepo.UpdateRefreshTokenAsync(user.user_id, refreshToken, refreshTokenExpire);

            _logger.LogInformation("Đăng nhập thành công [{username}]", loginDto.username);

            return new AuthResponseDto
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = ACCESS_TOKEN_EXPIRE_MINUTES * 60,
                refresh_token = refreshToken,
                user = MapToUserDto(user)
            };
        }

        /// <inheritdoc />
        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            // Kiểm tra username đã tồn tại
            if (await _userRepo.IsUsernameExistsAsync(registerDto.username))
            {
                throw new BusinessException("Tên đăng nhập đã tồn tại", 400);
            }

            // Kiểm tra email đã tồn tại
            if (await _userRepo.IsEmailExistsAsync(registerDto.email))
            {
                throw new BusinessException("Email đã được sử dụng", 400);
            }

            // Hash mật khẩu
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.password);

            // Tạo entity
            var user = new UserEntity
            {
                user_id = Guid.NewGuid(),
                username = registerDto.username,
                password_hash = passwordHash,
                email = registerDto.email,
                full_name = registerDto.full_name,
                phone_number = registerDto.phone_number,
                is_active = true,
                is_verified = false,
                role_code = "USER",
                created_date = DateTime.UtcNow,
                created_by = registerDto.username
            };

            // Lưu vào DB
            await _userRepo.InsertAsync(user);

            _logger.LogInformation("Đăng ký thành công [{username}]", registerDto.username);

            return MapToUserDto(user);
        }

        /// <inheritdoc />
        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            // Tìm user có refresh token này
            var user = await FindUserByRefreshTokenAsync(refreshTokenDto.refresh_token);

            if (user == null)
            {
                throw new BusinessException("Refresh token không hợp lệ", 401);
            }

            if (!user.is_active)
            {
                throw new BusinessException("Tài khoản đã bị khóa", 401);
            }

            if (user.refresh_token_expire < DateTime.UtcNow)
            {
                throw new BusinessException("Refresh token đã hết hạn", 401);
            }

            // Tạo tokens mới
            var accessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();
            var refreshTokenExpire = DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRE_DAYS);

            // Cập nhật refresh token
            await _userRepo.UpdateRefreshTokenAsync(user.user_id, newRefreshToken, refreshTokenExpire);

            return new AuthResponseDto
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = ACCESS_TOKEN_EXPIRE_MINUTES * 60,
                refresh_token = newRefreshToken,
                user = MapToUserDto(user)
            };
        }

        /// <inheritdoc />
        public async Task LogoutAsync(Guid userId)
        {
            // Xóa refresh token
            await _userRepo.UpdateRefreshTokenAsync(userId, null, (DateTime?)null);
            _logger.LogInformation("Đăng xuất thành công [userId: {userId}]", userId);
        }

        /// <inheritdoc />
        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                throw new BusinessException("Không tìm thấy người dùng", 404);
            }

            // Kiểm tra mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.current_password, user.password_hash))
            {
                throw new BusinessException("Mật khẩu hiện tại không đúng", 400);
            }

            // Hash mật khẩu mới
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.new_password);
            user.password_hash = newPasswordHash;
            user.modified_date = DateTime.UtcNow;
            user.modified_by = user.username;

            await _userRepo.UpdateAsync(user);

            _logger.LogInformation("Đổi mật khẩu thành công [userId: {userId}]", userId);
        }

        /// <inheritdoc />
        public async Task<UserDto> GetCurrentUserAsync(Guid userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                throw new BusinessException("Không tìm thấy người dùng", 404);
            }

            return MapToUserDto(user);
        }

        /// <inheritdoc />
        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto googleDto)
        {
            // Tìm user theo email
            var user = await _userRepo.GetByEmailAsync(googleDto.email);

            // Nếu chưa có user thì tạo mới
            if (user == null)
            {
                user = new UserEntity
                {
                    user_id = Guid.NewGuid(),
                    username = googleDto.email.Split('@')[0],
                    email = googleDto.email,
                    full_name = googleDto.full_name,
                    password_hash = null, // Không có password cho Google login
                    google_id = googleDto.google_token,
                    is_active = true,
                    is_verified = true,
                    role_code = "USER",
                    created_date = DateTime.UtcNow,
                    created_by = "Google"
                };

                await _userRepo.InsertAsync(user);
                _logger.LogInformation("Tạo tài khoản Google mới [{email}]", googleDto.email);
            }
            else
            {
                // Cập nhật google_id nếu chưa có
                if (string.IsNullOrEmpty(user.google_id))
                {
                    user.google_id = googleDto.google_token;
                    await _userRepo.UpdateAsync(user);
                }
            }

            // Tạo tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpire = DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRE_DAYS);

            // Lưu refresh token
            await _userRepo.UpdateRefreshTokenAsync(user.user_id, refreshToken, refreshTokenExpire);

            _logger.LogInformation("Đăng nhập Google thành công [{email}]", googleDto.email);

            return new AuthResponseDto
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = ACCESS_TOKEN_EXPIRE_MINUTES * 60,
                refresh_token = refreshToken,
                user = MapToUserDto(user)
            };
        }

        #region Private Methods

        /// <summary>
        /// Tạo JWT Access Token
        /// </summary>
        private string GenerateAccessToken(UserEntity user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SECRET_KEY));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.user_id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.email),
                new Claim("username", user.username),
                new Claim("role", user.role_code),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: ISSUER,
                audience: AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(ACCESS_TOKEN_EXPIRE_MINUTES),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Tạo Refresh Token ngẫu nhiên
        /// </summary>
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Tìm user theo refresh token
        /// </summary>
        private async Task<UserEntity> FindUserByRefreshTokenAsync(string refreshToken)
        {
            return await _userRepo.GetByRefreshTokenAsync(refreshToken);
        }

        /// <summary>
        /// Map Entity sang DTO
        /// </summary>
        private UserDto MapToUserDto(UserEntity user)
        {
            return new UserDto
            {
                user_id = user.user_id,
                username = user.username,
                email = user.email,
                full_name = user.full_name,
                phone_number = user.phone_number,
                avatar_url = user.avatar_url,
                is_active = user.is_active,
                role_code = user.role_code
            };
        }

        #endregion
    }
}