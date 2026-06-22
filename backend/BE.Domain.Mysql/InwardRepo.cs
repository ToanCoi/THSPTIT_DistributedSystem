using Dapper;
using BE.Domain.DI.Inward;
using BE.Domain.Entities;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Repository implementation cho phiếu nhập kho (MySQL)
    /// </summary>
    public class InwardRepo : IInwardRepo
    {
        private readonly string _connectionString;

        public InwardRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        public async Task<InwardEntity> GetByIdAsync(Guid inwardId)
        {
            const string sql = @"
                SELECT inward_id, product_id, stock_id, quantity, unit_price, selling_price,
                       supplier, invoice_date, created_date, created_by
                FROM inwards
                WHERE inward_id = @inwardId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<InwardEntity>(sql, new { inwardId = inwardId.ToString() });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<InwardEntity>> GetAllAsync()
        {
            const string sql = @"
                SELECT inward_id, product_id, stock_id, quantity, unit_price, selling_price,
                       supplier, invoice_date, created_date, created_by
                FROM inwards
                ORDER BY created_date DESC";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryAsync<InwardEntity>(sql);
        }

        /// <inheritdoc />
        public async Task<bool> InsertAsync(InwardEntity inward)
        {
            const string sql = @"
                INSERT INTO inwards (inward_id, product_id, stock_id, quantity, unit_price, selling_price,
                                     supplier, invoice_date, created_date, created_by)
                VALUES (@inward_id, @product_id, @stock_id, @quantity, @unit_price, @selling_price,
                        @supplier, @invoice_date, @created_date, @created_by)";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                inward_id = inward.inward_id.ToString(),
                product_id = inward.product_id.ToString(),
                stock_id = inward.stock_id.ToString(),
                inward.quantity,
                inward.unit_price,
                inward.selling_price,
                inward.supplier,
                inward.invoice_date,
                inward.created_date,
                inward.created_by
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<decimal?> GetLatestInwardPriceAsync(Guid productId)
        {
            const string sql = @"
                SELECT unit_price FROM inwards
                WHERE product_id = @productId
                ORDER BY created_date DESC
                LIMIT 1";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<decimal?>(sql, new { productId = productId.ToString() });
        }

        /// <inheritdoc />
        public async Task<decimal?> GetLatestSellingPriceAsync(Guid productId)
        {
            const string sql = @"
                SELECT selling_price FROM inwards
                WHERE product_id = @productId AND selling_price > 0
                ORDER BY created_date DESC
                LIMIT 1";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<decimal?>(sql, new { productId = productId.ToString() });
        }
    }
}