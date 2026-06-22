using BE.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.DI.Order
{
    /// <summary>
    /// Interface repository đơn hàng
    /// </summary>
    public interface IOrderRepo
    {
        /// <summary>
        /// Lấy đơn hàng theo ID
        /// </summary>
        Task<OrderEntity> GetByIdAsync(Guid orderId);

        /// <summary>
        /// Lấy tất cả đơn hàng
        /// </summary>
        Task<IEnumerable<OrderEntity>> GetAllAsync();

        /// <summary>
        /// Lấy đơn hàng theo mã
        /// </summary>
        Task<OrderEntity> GetByCodeAsync(string orderCode);

        /// <summary>
        /// Thêm đơn hàng mới
        /// </summary>
        Task<bool> InsertAsync(OrderEntity order);

        /// <summary>
        /// Cập nhật đơn hàng
        /// </summary>
        Task<bool> UpdateAsync(OrderEntity order);
    }

    /// <summary>
    /// Interface repository chi tiết đơn hàng
    /// </summary>
    public interface IOrderItemRepo
    {
        /// <summary>
        /// Lấy chi tiết đơn hàng theo order ID
        /// </summary>
        Task<IEnumerable<OrderItemEntity>> GetByOrderIdAsync(Guid orderId);

        /// <summary>
        /// Thêm chi tiết đơn hàng
        /// </summary>
        Task<bool> InsertAsync(OrderItemEntity orderItem);

        /// <summary>
        /// Thêm nhiều chi tiết đơn hàng
        /// </summary>
        Task<bool> InsertManyAsync(IEnumerable<OrderItemEntity> orderItems);

        /// <summary>
        /// Xóa chi tiết đơn hàng theo order ID
        /// </summary>
        Task<bool> DeleteByOrderIdAsync(Guid orderId);
    }
}