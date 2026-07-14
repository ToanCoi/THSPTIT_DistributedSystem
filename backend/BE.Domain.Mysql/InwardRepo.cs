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
                SELECT i.inward_id, i.product_id, i.stock_id, i.quantity, i.unit_price, i.selling_price,
                       i.supplier, i.invoice_date, i.created_date, i.created_by,
                       p.product_name, s.stock_name
                FROM inwards i
                LEFT JOIN products p ON i.product_id = p.product_id
                LEFT JOIN stocks s ON i.stock_id = s.stock_id
                WHERE i.inward_id = @inwardId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<InwardEntity>(sql, new { inwardId = inwardId.ToString() });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<InwardEntity>> GetAllAsync()
        {
            const string sql = @"
                SELECT i.inward_id, i.product_id, i.stock_id, i.quantity, i.unit_price, i.selling_price,
                       i.supplier, i.invoice_date, i.created_date, i.created_by,
                       p.product_name, s.stock_name
                FROM inwards i
                LEFT JOIN products p ON i.product_id = p.product_id
                LEFT JOIN stocks s ON i.stock_id = s.stock_id
                ORDER BY i.created_date DESC";

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
        public async Task<bool> UpdateAsync(InwardEntity inward)
        {
            const string sql = @"
                UPDATE inwards
                SET product_id = @product_id,
                    stock_id = @stock_id,
                    quantity = @quantity,
                    unit_price = @unit_price,
                    selling_price = @selling_price,
                    supplier = @supplier,
                    invoice_date = @invoice_date
                WHERE inward_id = @inward_id";

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
                inward.invoice_date
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

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid inwardId)
        {
            const string sql = "DELETE FROM inwards WHERE inward_id = @inwardId";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new { inwardId = inwardId.ToString() });
            return rows > 0;
        }
    }
}