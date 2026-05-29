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
                SELECT outward_id, order_id, product_id, stock_id, quantity, unit_price,
                       outward_date, created_date, created_by
                FROM outwards
                WHERE outward_id = @outwardId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<OutwardEntity>(sql, new { outwardId = outwardId.ToString() });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OutwardEntity>> GetAllAsync()
        {
            const string sql = @"
                SELECT outward_id, order_id, product_id, stock_id, quantity, unit_price,
                       outward_date, created_date, created_by
                FROM outwards
                ORDER BY created_date DESC";

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
    }
}