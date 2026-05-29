using Dapper;
using BE.Domain.DI.Customer;
using BE.Domain.Entities;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Repository implementation cho khách hàng (MySQL)
    /// </summary>
    public class CustomerRepo : ICustomerRepo
    {
        private readonly string _connectionString;

        public CustomerRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        public async Task<CustomerEntity> GetByIdAsync(Guid customerId)
        {
            const string sql = @"
                SELECT customer_id, user_id, full_name, phone, email, address,
                       created_date, created_by, modified_date, modified_by
                FROM customers
                WHERE customer_id = @customerId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<CustomerEntity>(sql, new { customerId = customerId.ToString() });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CustomerEntity>> GetAllAsync()
        {
            const string sql = @"
                SELECT customer_id, user_id, full_name, phone, email, address,
                       created_date, created_by, modified_date, modified_by
                FROM customers
                ORDER BY created_date DESC";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryAsync<CustomerEntity>(sql);
        }

        /// <inheritdoc />
        public async Task<bool> InsertAsync(CustomerEntity customer)
        {
            const string sql = @"
                INSERT INTO customers (customer_id, user_id, full_name, phone, email, address, created_date, created_by)
                VALUES (@customer_id, @user_id, @full_name, @phone, @email, @address, @created_date, @created_by)";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                customer_id = customer.customer_id.ToString(),
                user_id = customer.user_id?.ToString(),
                customer.full_name,
                customer.phone,
                customer.email,
                customer.address,
                customer.created_date,
                customer.created_by
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(CustomerEntity customer)
        {
            const string sql = @"
                UPDATE customers
                SET full_name = @full_name,
                    phone = @phone,
                    email = @email,
                    address = @address,
                    modified_date = @modified_date,
                    modified_by = @modified_by
                WHERE customer_id = @customer_id";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                customer_id = customer.customer_id.ToString(),
                customer.full_name,
                customer.phone,
                customer.email,
                customer.address,
                customer.modified_date,
                customer.modified_by
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid customerId)
        {
            const string sql = "DELETE FROM customers WHERE customer_id = @customerId";
            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new { customerId = customerId.ToString() });
            return rows > 0;
        }
    }
}