using Dapper;
using BE.Domain.DI.Order;
using BE.Domain.Entities;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BE.Domain.Mysql
{
    /// <summary>
    /// Repository implementation cho đơn hàng (MySQL)
    /// </summary>
    public class OrderRepo : IOrderRepo
    {
        private readonly string _connectionString;

        public OrderRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        public async Task<OrderEntity> GetByIdAsync(Guid orderId)
        {
            const string sql = @"
                SELECT o.order_id, o.customer_id, o.order_code, o.total_amount, o.status,
                       o.order_date, o.created_date, o.created_by, o.stock_id,
                       c.full_name AS customer_name, s.stock_name AS stock_name
                FROM orders o
                LEFT JOIN customers c ON o.customer_id = c.customer_id
                LEFT JOIN stocks s ON o.stock_id = s.stock_id
                WHERE o.order_id = @orderId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<OrderEntity>(sql, new { orderId = orderId.ToString() });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrderEntity>> GetAllAsync()
        {
            const string sql = @"
                SELECT o.order_id, o.customer_id, o.order_code, o.total_amount, o.status,
                       o.order_date, o.created_date, o.created_by, o.stock_id,
                       c.full_name AS customer_name, s.stock_name AS stock_name
                FROM orders o
                LEFT JOIN customers c ON o.customer_id = c.customer_id
                LEFT JOIN stocks s ON o.stock_id = s.stock_id
                ORDER BY o.created_date DESC";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryAsync<OrderEntity>(sql);
        }

        /// <inheritdoc />
        public async Task<OrderEntity> GetByCodeAsync(string orderCode)
        {
            const string sql = @"
                SELECT o.order_id, o.customer_id, o.order_code, o.total_amount, o.status,
                       o.order_date, o.created_date, o.created_by, o.stock_id,
                       c.full_name AS customer_name, s.stock_name AS stock_name
                FROM orders o
                LEFT JOIN customers c ON o.customer_id = c.customer_id
                LEFT JOIN stocks s ON o.stock_id = s.stock_id
                WHERE o.order_code = @orderCode";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<OrderEntity>(sql, new { orderCode });
        }

        /// <inheritdoc />
        public async Task<bool> InsertAsync(OrderEntity order)
        {
            const string sql = @"
                INSERT INTO orders (order_id, customer_id, stock_id, order_code, total_amount, status,
                                    order_date, created_date, created_by)
                VALUES (@order_id, @customer_id, @stock_id, @order_code, @total_amount, @status,
                        @order_date, @created_date, @created_by)";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                order_id = order.order_id.ToString(),
                customer_id = order.customer_id.ToString(),
                stock_id = order.stock_id == Guid.Empty ? null : order.stock_id.ToString(),
                order.order_code,
                order.total_amount,
                order.status,
                order.order_date,
                order.created_date,
                order.created_by
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(OrderEntity order)
        {
            const string sql = @"
                UPDATE orders
                SET customer_id = @customer_id,
                    stock_id = @stock_id,
                    total_amount = @total_amount,
                    status = @status,
                    order_date = @order_date
                WHERE order_id = @order_id";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                order_id = order.order_id.ToString(),
                customer_id = order.customer_id.ToString(),
                stock_id = order.stock_id == Guid.Empty ? null : order.stock_id.ToString(),
                order.total_amount,
                order.status,
                order.order_date
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid orderId)
        {
            const string sql = @"DELETE FROM orders WHERE order_id = @orderId";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new { orderId = orderId.ToString() });
            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<long> GetNextOrderCodeAsync()
        {
            const string updateSql = @"
                UPDATE order_sequence
                SET current_value = current_value + 1, updated_date = NOW()
                WHERE sequence_name = 'order_code'";
            const string selectSql = @"
                SELECT current_value FROM order_sequence WHERE sequence_name = 'order_code' FOR UPDATE";

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await connection.ExecuteAsync(updateSql, transaction: transaction);
                var next = await connection.QueryFirstOrDefaultAsync<long>(selectSql, transaction: transaction);
                await transaction.CommitAsync();
                return next;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    /// <summary>
    /// Repository implementation cho chi tiết đơn hàng (MySQL)
    /// </summary>
    public class OrderItemRepo : IOrderItemRepo
    {
        private readonly string _connectionString;

        public OrderItemRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrderItemEntity>> GetByOrderIdAsync(Guid orderId)
        {
            const string sql = @"
                SELECT order_item_id, order_id, product_id, quantity, unit_price,
                       created_date, created_by
                FROM order_items
                WHERE order_id = @orderId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryAsync<OrderItemEntity>(sql, new { orderId = orderId.ToString() });
        }

        /// <inheritdoc />
        public async Task<bool> InsertAsync(OrderItemEntity orderItem)
        {
            const string sql = @"
                INSERT INTO order_items (order_item_id, order_id, product_id, quantity, unit_price,
                                         created_date, created_by)
                VALUES (@order_item_id, @order_id, @product_id, @quantity, @unit_price,
                        @created_date, @created_by)";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                order_item_id = orderItem.order_item_id.ToString(),
                order_id = orderItem.order_id.ToString(),
                product_id = orderItem.product_id.ToString(),
                orderItem.quantity,
                orderItem.unit_price,
                orderItem.created_date,
                orderItem.created_by
            });

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> InsertManyAsync(IEnumerable<OrderItemEntity> orderItems)
        {
            const string sql = @"
                INSERT INTO order_items (order_item_id, order_id, product_id, quantity, unit_price,
                                         created_date, created_by)
                VALUES (@order_item_id, @order_id, @product_id, @quantity, @unit_price,
                        @created_date, @created_by)";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, orderItems.Select(item => new
            {
                order_item_id = item.order_item_id.ToString(),
                order_id = item.order_id.ToString(),
                product_id = item.product_id.ToString(),
                item.quantity,
                item.unit_price,
                item.created_date,
                item.created_by
            }));

            return rows > 0;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteByOrderIdAsync(Guid orderId)
        {
            const string sql = @"
                DELETE FROM order_items
                WHERE order_id = @orderId";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new { orderId = orderId.ToString() });
            return rows > 0;
        }
    }
}