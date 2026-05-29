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
                SELECT order_id, customer_id, order_code, total_amount, status,
                       order_date, created_date, created_by
                FROM orders
                WHERE order_id = @orderId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<OrderEntity>(sql, new { orderId = orderId.ToString() });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrderEntity>> GetAllAsync()
        {
            const string sql = @"
                SELECT order_id, customer_id, order_code, total_amount, status,
                       order_date, created_date, created_by
                FROM orders
                ORDER BY created_date DESC";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryAsync<OrderEntity>(sql);
        }

        /// <inheritdoc />
        public async Task<OrderEntity> GetByCodeAsync(string orderCode)
        {
            const string sql = @"
                SELECT order_id, customer_id, order_code, total_amount, status,
                       order_date, created_date, created_by
                FROM orders
                WHERE order_code = @orderCode";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<OrderEntity>(sql, new { orderCode });
        }

        /// <inheritdoc />
        public async Task<bool> InsertAsync(OrderEntity order)
        {
            const string sql = @"
                INSERT INTO orders (order_id, customer_id, order_code, total_amount, status,
                                    order_date, created_date, created_by)
                VALUES (@order_id, @customer_id, @order_code, @total_amount, @status,
                        @order_date, @created_date, @created_by)";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                order_id = order.order_id.ToString(),
                customer_id = order.customer_id.ToString(),
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
                    total_amount = @total_amount,
                    status = @status,
                    order_date = @order_date
                WHERE order_id = @order_id";

            using var connection = new MySqlConnection(_connectionString);
            var rows = await connection.ExecuteAsync(sql, new
            {
                order_id = order.order_id.ToString(),
                customer_id = order.customer_id.ToString(),
                order.total_amount,
                order.status,
                order.order_date
            });

            return rows > 0;
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
    }
}