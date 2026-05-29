using BE.Domain.DI.User;
using BE.Domain.Entities;
using Dapper;
using MySqlConnector;
using System;
using System.Threading.Tasks;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Repository implementation cho user (MySQL)
    /// </summary>
    public class UserRepo : IUserRepo
    {
        private readonly string _connectionString;

        static UserRepo()
        {
            DapperSetup.RegisterGuidTypeHandler();
        }

        public UserRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        public async Task<UserEntity> GetByUsernameAsync(string username)
        {
            const string sql = @"
                SELECT user_id, username, password_hash, email, full_name,
                       phone_number, avatar_url, is_active, is_verified,
                       refresh_token, refresh_token_expire, role_code,
                       created_date, created_by, modified_date, modified_by
                FROM users
                WHERE username = @username";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { username });
        }

        /// <inheritdoc />
        public async Task<UserEntity> GetByEmailAsync(string email)
        {
            const string sql = @"
                SELECT user_id, username, password_hash, email, full_name,
                       phone_number, avatar_url, is_active, is_verified,
                       refresh_token, refresh_token_expire, role_code,
                       created_date, created_by, modified_date, modified_by
                FROM users
                WHERE email = @email";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { email });
        }

        /// <inheritdoc />
        public async Task<UserEntity> GetByIdAsync(Guid userId)
        {
            const string sql = @"
                SELECT user_id, username, password_hash, email, full_name,
                       phone_number, avatar_url, is_active, is_verified,
                       refresh_token, refresh_token_expire, role_code,
                       created_date, created_by, modified_date, modified_by
                FROM users
                WHERE user_id = @userId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { userId = userId.ToString() });
        }

        /// <inheritdoc />
        public async Task<UserEntity> GetByRefreshTokenAsync(string refreshToken)
        {
            const string sql = @"
                SELECT user_id, username, password_hash, email, full_name,
                       phone_number, avatar_url, is_active, is_verified,
                       refresh_token, refresh_token_expire, role_code,
                       created_date, created_by, modified_date, modified_by
                FROM users
                WHERE refresh_token = @refreshToken";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UserEntity>(sql, new { refreshToken });
        }

        /// <inheritdoc />
        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            const string sql = "SELECT COUNT(1) FROM users WHERE username = @username";
            using var connection = new MySqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(sql, new { username });
            return count > 0;
        }

        /// <inheritdoc />
        public async Task<bool> IsEmailExistsAsync(string email)
        {
            const string sql = "SELECT COUNT(1) FROM users WHERE email = @email";
            using var connection = new MySqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(sql, new { email });
            return count > 0;
        }

        /// <inheritdoc />
        public async Task UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime? expireTime)
        {
            const string sql = @"
                UPDATE users
                SET refresh_token = @refreshToken,
                    refresh_token_expire = @expireTime
                WHERE user_id = @userId";

            using var connection = new MySqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                userId = userId.ToString(),
                refreshToken,
                expireTime
            });
        }

        /// <inheritdoc />
        public async Task<bool> InsertAsync(UserEntity user)
        {
            const string sql = @"
                INSERT INTO users (user_id, username, password_hash, email, full_name,
                                 phone_number, avatar_url, is_active, is_verified,
                                 role_code, created_date, created_by)
                VALUES (@user_id, @username, @password_hash, @email, @full_name,
                        @phone_number, @avatar_url, @is_active, @is_verified,
                        @role_code, @created_date, @created_by)";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                user_id = user.user_id.ToString(),
                user.username,
                user.password_hash,
                user.email,
                user.full_name,
                user.phone_number,
                user.avatar_url,
                is_active = user.is_active ? 1 : 0,
                is_verified = user.is_verified ? 1 : 0,
                user.role_code,
                created_date = user.created_date,
                user.created_by
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(UserEntity user)
        {
            const string sql = @"
                UPDATE users
                SET full_name = @full_name,
                    phone_number = @phone_number,
                    avatar_url = @avatar_url,
                    is_active = @is_active,
                    is_verified = @is_verified,
                    role_code = @role_code,
                    modified_date = @modified_date,
                    modified_by = @modified_by
                WHERE user_id = @user_id";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                user_id = user.user_id.ToString(),
                user.full_name,
                user.phone_number,
                user.avatar_url,
                is_active = user.is_active ? 1 : 0,
                is_verified = user.is_verified ? 1 : 0,
                user.role_code,
                user.modified_date,
                user.modified_by
            });

            return rows > 0;
        }
    }
}