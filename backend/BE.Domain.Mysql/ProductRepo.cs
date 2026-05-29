using Dapper;
using BE.Domain.DI.Product;
using BE.Domain.Entities;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Repository implementation cho sản phẩm (MySQL)
    /// </summary>
    public class ProductRepo : IProductRepo
    {
        private readonly string _connectionString;

        public ProductRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        public async Task<ProductEntity> GetByIdAsync(Guid productId)
        {
            const string sql = @"
                SELECT product_id, product_code, product_name, price, unit,
                       created_date, created_by, modified_date, modified_by
                FROM products
                WHERE product_id = @productId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ProductEntity>(sql, new { productId = productId.ToString() });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ProductEntity>> GetAllAsync()
        {
            const string sql = @"
                SELECT product_id, product_code, product_name, price, unit,
                       created_date, created_by, modified_date, modified_by
                FROM products
                ORDER BY created_date DESC";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryAsync<ProductEntity>(sql);
        }

        /// <inheritdoc />
        public async Task<bool> InsertAsync(ProductEntity product)
        {
            const string sql = @"
                INSERT INTO products (product_id, product_code, product_name, price, unit, created_date, created_by)
                VALUES (@product_id, @product_code, @product_name, @price, @unit, @created_date, @created_by)";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                product_id = product.product_id.ToString(),
                product.product_code,
                product.product_name,
                product.price,
                product.unit,
                product.created_date,
                product.created_by
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(ProductEntity product)
        {
            const string sql = @"
                UPDATE products
                SET product_code = @product_code,
                    product_name = @product_name,
                    price = @price,
                    unit = @unit,
                    modified_date = @modified_date,
                    modified_by = @modified_by
                WHERE product_id = @product_id";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                product_id = product.product_id.ToString(),
                product.product_code,
                product.product_name,
                product.price,
                product.unit,
                product.modified_date,
                product.modified_by
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid productId)
        {
            const string sql = "DELETE FROM products WHERE product_id = @productId";
            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new { productId = productId.ToString() });
            return rows > 0;
        }
    }
}