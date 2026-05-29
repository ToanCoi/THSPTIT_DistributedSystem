using BE.HostBase.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BE.HostBase.Services
{
    /// <summary>
    /// Service xử lý JWT token - inject vào tất cả service cần verify token
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private const int ACCESS_TOKEN_EXPIRE_MINUTES = 60;

        public JwtService(IHttpContextAccessor httpContextAccessor, string secretKey, string issuer, string audience)
        {
            _httpContextAccessor = httpContextAccessor;
            _secretKey = secretKey;
            _issuer = issuer;
            _audience = audience;
        }

        /// <inheritdoc />
        public Guid? GetCurrentUserId()
        {
            var userIdClaim = GetClaim(ClaimTypes.NameIdentifier) ?? GetClaim("sub");
            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        /// <inheritdoc />
        public string? GetCurrentUsername()
        {
            return GetClaim("username");
        }

        /// <inheritdoc />
        public string? GetCurrentRole()
        {
            return GetClaim("role");
        }

        /// <inheritdoc />
        public bool IsTokenValid()
        {
            var token = GetTokenFromHeader();
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return true;
            }
            catch
            {
                return false;
            }
        }

        #region Private Methods

        /// <summary>
        /// Lấy claim từ token
        /// </summary>
        private string? GetClaim(string claimType)
        {
            var token = GetTokenFromHeader();
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy token từ Authorization header
        /// </summary>
        private string? GetTokenFromHeader()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            return authHeader.Substring("Bearer ".Length).Trim();
        }

        #endregion
    }
}