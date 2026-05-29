using Dapper;
using BE.Domain.DI.Stock;
using BE.Domain.Entities;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Repository implementation cho kho (MySQL)
    /// </summary>
    public class StockRepo : IStockRepo
    {
        private readonly string _connectionString;

        public StockRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        public async Task<StockEntity> GetByIdAsync(Guid stockId)
        {
            const string sql = @"
                SELECT stock_id, stock_code, stock_name, address,
                       created_date, created_by, modified_date, modified_by
                FROM stocks
                WHERE stock_id = @stockId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StockEntity>(sql, new { stockId = stockId.ToString() });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StockEntity>> GetAllAsync()
        {
            const string sql = @"
                SELECT stock_id, stock_code, stock_name, address,
                       created_date, created_by, modified_date, modified_by
                FROM stocks
                ORDER BY created_date DESC";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryAsync<StockEntity>(sql);
        }
    }
}