using BE.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace BE.Domain.DI.User
{
    /// <summary>
    /// Interface repository cho người dùng
    /// </summary>
    public interface IUserRepo
    {
        /// <summary>
        /// Lấy user theo username
        /// </summary>
        Task<UserEntity> GetByUsernameAsync(string username);

        /// <summary>
        /// Lấy user theo email
        /// </summary>
        Task<UserEntity> GetByEmailAsync(string email);

        /// <summary>
        /// Lấy user theo ID
        /// </summary>
        Task<UserEntity> GetByIdAsync(Guid userId);

        /// <summary>
        /// Lấy user theo refresh token
        /// </summary>
        Task<UserEntity> GetByRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Kiểm tra username đã tồn tại chưa
        /// </summary>
        Task<bool> IsUsernameExistsAsync(string username);

        /// <summary>
        /// Kiểm tra email đã tồn tại chưa
        /// </summary>
        Task<bool> IsEmailExistsAsync(string email);

        /// <summary>
        /// Cập nhật refresh token
        /// </summary>
        Task UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime? expireTime);

        /// <summary>
        /// Tạo mới user
        /// </summary>
        Task<bool> InsertAsync(UserEntity user);

        /// <summary>
        /// Cập nhật thông tin user
        /// </summary>
        Task<bool> UpdateAsync(UserEntity user);
    }
}