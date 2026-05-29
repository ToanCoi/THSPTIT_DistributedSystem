using BE.HostBase.Interfaces;
using BE.HostBase.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BE.HostBase.Extensions
{
    /// <summary>
    /// Extension để inject JWT authentication vào tất cả các host
    /// </summary>
    public static class JwtAuthorizationExtensions
    {
        /// <summary>
        /// Cấu hình JWT Authentication cho host
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="secretKey">Secret key để sign token</param>
        /// <param name="issuer">Issuer (VD: BE.AuthApi)</param>
        /// <param name="audience">Audience (VD: BE.Client)</param>
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            string secretKey,
            string issuer,
            string audience)
        {
            // Cấu hình JWT Bearer Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Đăng ký HttpContextAccessor (để JwtService đọc header)
            services.AddHttpContextAccessor();

            // Đăng ký JwtService với các tham số config
            services.AddScoped<IJwtService>(sp =>
            {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                return new JwtService(httpContextAccessor, secretKey, issuer, audience);
            });

            return services;
        }

        /// <summary>
        /// Cấu hình JWT Authentication từ config
        /// </summary>
        public static IServiceCollection AddJwtAuthenticationFromConfig(
            this IServiceCollection services,
            string secretKey,
            string issuer,
            string audience)
        {
            return AddJwtAuthentication(services, secretKey, issuer, audience);
        }
    }
}