using BE.Application.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Contracts.Interfaces.Order
{
    /// <summary>
    /// Interface service đơn hàng
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Lấy đơn hàng theo ID
        /// </summary>
        Task<OrderDto> GetByIdAsync(Guid orderId);

        /// <summary>
        /// Lấy tất cả đơn hàng
        /// </summary>
        Task<IEnumerable<OrderDto>> GetAllAsync();

        /// <summary>
        /// Lấy danh sách phân trang
        /// </summary>
        Task<PagingResult<OrderDto>> GetAllPagingAsync(PagingFilterDto filter);

        /// <summary>
        /// Tạo đơn hàng mới (publish Kafka message)
        /// </summary>
        Task<OrderDto> CreateAsync(OrderCreateDto dto);

        /// <summary>
        /// Cập nhật trạng thái đơn hàng
        /// </summary>
        Task<OrderDto> UpdateStatusAsync(Guid orderId, string status);

        /// <summary>
        /// Cập nhật đơn hàng đầy đủ
        /// </summary>
        Task<OrderDto> UpdateAsync(Guid orderId, OrderCreateDto dto);
    }

    /// <summary>
    /// DTO đơn hàng
    /// </summary>
    public class OrderDto
    {
        public Guid order_id { get; set; }
        public Guid customer_id { get; set; }
        public string order_code { get; set; }
        public decimal total_amount { get; set; }
        public string status { get; set; }
        public DateTime order_date { get; set; }
        public DateTime created_date { get; set; }
        public List<OrderItemDto> items { get; set; } = new List<OrderItemDto>();
    }

    /// <summary>
    /// DTO chi tiết đơn hàng
    /// </summary>
    public class OrderItemDto
    {
        public Guid order_item_id { get; set; }
        public Guid product_id { get; set; }
        public decimal quantity { get; set; }
        public decimal unit_price { get; set; }
    }

    /// <summary>
    /// DTO tạo đơn hàng
    /// </summary>
    public class OrderCreateDto
    {
        public Guid customer_id { get; set; }
        public Guid stock_id { get; set; }
        public DateTime order_date { get; set; }
        public List<OrderItemCreateDto> items { get; set; } = new List<OrderItemCreateDto>();
    }

    /// <summary>
    /// DTO tạo chi tiết đơn hàng
    /// </summary>
    public class OrderItemCreateDto
    {
        public Guid product_id { get; set; }
        public decimal quantity { get; set; }
        public decimal unit_price { get; set; }
    }
}
