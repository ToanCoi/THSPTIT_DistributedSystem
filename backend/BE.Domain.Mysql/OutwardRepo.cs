using Dapper;
using BE.Domain.DI.Outward;
using BE.Domain.Entities;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Repository implementation cho phiếu xuất kho (MySQL)
    /// </summary>
    public class OutwardRepo : IOutwardRepo
    {
        private readonly string _connectionString;

        public OutwardRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        public async Task<OutwardEntity> GetByIdAsync(Guid outwardId)
        {
            const string sql = @"
                SELECT o.outward_id, o.order_id, o.product_id, o.stock_id, o.quantity, o.unit_price,
                       o.outward_date, o.created_date, o.created_by,
                       p.product_name, s.stock_name, ord.order_code
                FROM outwards o
                LEFT JOIN products p ON o.product_id = p.product_id
                LEFT JOIN stocks s ON o.stock_id = s.stock_id
                LEFT JOIN orders ord ON o.order_id = ord.order_id
                WHERE o.outward_id = @outwardId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<OutwardEntity>(sql, new { outwardId = outwardId.ToString() });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OutwardEntity>> GetAllAsync()
        {
            const string sql = @"
                SELECT o.outward_id, o.order_id, o.product_id, o.stock_id, o.quantity, o.unit_price,
                       o.outward_date, o.created_date, o.created_by,
                       p.product_name, s.stock_name, ord.order_code
                FROM outwards o
                LEFT JOIN products p ON o.product_id = p.product_id
                LEFT JOIN stocks s ON o.stock_id = s.stock_id
                LEFT JOIN orders ord ON o.order_id = ord.order_id
                ORDER BY o.created_date DESC";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryAsync<OutwardEntity>(sql);
        }

        /// <inheritdoc />
        public async Task<bool> InsertAsync(OutwardEntity outward)
        {
            const string sql = @"
                INSERT INTO outwards (outward_id, order_id, product_id, stock_id, quantity, unit_price,
                                      outward_date, created_date, created_by)
                VALUES (@outward_id, @order_id, @product_id, @stock_id, @quantity, @unit_price,
                        @outward_date, @created_date, @created_by)";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                outward_id = outward.outward_id.ToString(),
                order_id = outward.order_id?.ToString(),
                product_id = outward.product_id.ToString(),
                stock_id = outward.stock_id.ToString(),
                outward.quantity,
                outward.unit_price,
                outward.outward_date,
                outward.created_date,
                outward.created_by
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(OutwardEntity outward)
        {
            const string sql = @"
                UPDATE outwards
                SET product_id = @product_id,
                    stock_id = @stock_id,
                    quantity = @quantity,
                    unit_price = @unit_price,
                    outward_date = @outward_date
                WHERE outward_id = @outward_id";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                outward_id = outward.outward_id.ToString(),
                product_id = outward.product_id.ToString(),
                stock_id = outward.stock_id.ToString(),
                outward.quantity,
                outward.unit_price,
                outward.outward_date
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<decimal?> GetLatestOutwardPriceAsync(Guid productId)
        {
            const string sql = @"
                SELECT unit_price FROM outwards
                WHERE product_id = @productId
                ORDER BY created_date DESC
                LIMIT 1";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<decimal?>(sql, new { productId = productId.ToString() });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OutwardEntity>> GetByOrderIdAsync(Guid orderId)
        {
            const string sql = @"
                SELECT outward_id, order_id, product_id, stock_id, quantity, unit_price,
                       outward_date, created_date, created_by
                FROM outwards
                WHERE order_id = @orderId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryAsync<OutwardEntity>(sql, new { orderId = orderId.ToString() });
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid outwardId)
        {
            const string sql = "DELETE FROM outwards WHERE outward_id = @outwardId";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new { outwardId = outwardId.ToString() });
            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteByOrderIdAsync(Guid orderId)
        {
            const string sql = "DELETE FROM outwards WHERE order_id = @orderId";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new { orderId = orderId.ToString() });
            return rows > 0;
        }
    }
}