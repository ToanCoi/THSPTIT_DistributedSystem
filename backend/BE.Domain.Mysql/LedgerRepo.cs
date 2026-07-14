using Dapper;
using BE.Domain.DI.Ledger;
using BE.Domain.Entities;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Repository implementation cho sổ cái tồn kho (MySQL)
    /// </summary>
    public class LedgerRepo : ILedgerRepo
    {
        private readonly string _connectionString;

        public LedgerRepo(string connectionString)
        {
            DapperSetup.RegisterGuidTypeHandler();
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        public async Task<bool> InsertAsync(LedgerEntity ledger)
        {
            const string sql = @"
                INSERT INTO led_inventory_item_ledger
                    (ledger_id, product_id, stock_id, inward_quantity, outward_quantity,
                     reference_id, reference_type, ledger_date, created_date, created_by)
                VALUES
                    (@ledger_id, @product_id, @stock_id, @inward_quantity, @outward_quantity,
                     @reference_id, @reference_type, @ledger_date, @created_date, @created_by)";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                ledger_id = ledger.ledger_id.ToString(),
                product_id = ledger.product_id.ToString(),
                stock_id = ledger.stock_id.ToString(),
                ledger.inward_quantity,
                ledger.outward_quantity,
                reference_id = ledger.reference_id.ToString(),
                ledger.reference_type,
                ledger.ledger_date,
                ledger.created_date,
                ledger.created_by
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> UpsertLedgerDateAsync(Guid productId, Guid stockId, decimal inwardQty, decimal outwardQty, DateTime ledgerDate)
        {
            const string sql = @"
                INSERT INTO led_inventory_item_ledger_date
                    (ledger_date_id, product_id, stock_id, inward_quantity, outward_quantity, ledger_date, created_date)
                VALUES
                    (@ledger_date_id, @product_id, @stock_id, @inward_quantity, @outward_quantity, @ledger_date, @created_date)
                ON DUPLICATE KEY UPDATE
                    inward_quantity = inward_quantity + @inward_quantity,
                    outward_quantity = outward_quantity + @outward_quantity";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                ledger_date_id = Guid.NewGuid().ToString(),
                product_id = productId.ToString(),
                stock_id = stockId.ToString(),
                inward_quantity = inwardQty,
                outward_quantity = outwardQty,
                ledger_date = ledgerDate.Date,
                created_date = DateTime.UtcNow
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> UpsertClosingAsync(Guid productId, Guid stockId, decimal quantity)
        {
            const string sql = @"
                INSERT INTO led_inventory_item_ledger_closing
                    (closing_id, product_id, stock_id, quantity, updated_date)
                VALUES
                    (@closing_id, @product_id, @stock_id, @quantity, @updated_date)
                ON DUPLICATE KEY UPDATE
                    quantity = quantity + @quantity,
                    updated_date = @updated_date";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                closing_id = Guid.NewGuid().ToString(),
                product_id = productId.ToString(),
                stock_id = stockId.ToString(),
                quantity = quantity,
                updated_date = DateTime.UtcNow
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<decimal> GetClosingQuantityAsync(Guid productId, Guid stockId)
        {
            const string sql = @"
                SELECT quantity FROM led_inventory_item_ledger_closing
                WHERE product_id = @productId AND stock_id = @stockId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<decimal>(sql, new
            {
                productId = productId.ToString(),
                stockId = stockId.ToString()
            });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<LedgerEntity>> GetByReferenceIdAsync(Guid referenceId)
        {
            const string sql = @"
                SELECT ledger_id, product_id, stock_id, inward_quantity, outward_quantity,
                       reference_id, reference_type, ledger_date, created_date, created_by
                FROM led_inventory_item_ledger
                WHERE reference_id = @referenceId
                ORDER BY created_date ASC";

            using var connection = new MySqlConnection(_connectionString);
            IEnumerable<LedgerEntity> result = await connection.QueryAsync<LedgerEntity>(sql, new { referenceId = referenceId.ToString() });
            return result;
        }

        /// <inheritdoc />
        public async Task<int> DeleteByReferenceIdAsync(Guid referenceId)
        {
            const string sql = @"DELETE FROM led_inventory_item_ledger WHERE reference_id = @referenceId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, new { referenceId = referenceId.ToString() });
        }
    }
}