using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace OrderApi.Controllers
{
    /// <summary>
    /// Controller quản lý đơn hàng
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        /// <summary>
        /// Khởi tạo OrdersController
        /// </summary>
        /// <param name="orderService">Service đơn hàng</param>
        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Lấy tất cả đơn hàng
        /// </summary>
        /// <returns>Danh sách đơn hàng</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _orderService.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách phân trang
        /// </summary>
        /// <param name="filter">Filter phân trang</param>
        /// <returns>Danh sách phân trang</returns>
        [HttpGet("paging")]
        public async Task<IActionResult> GetAllPaging([FromQuery] PagingFilterDto filter)
        {
            var result = await _orderService.GetAllPagingAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy đơn hàng theo ID
        /// </summary>
        /// <param name="id">ID đơn hàng</param>
        /// <returns>Đơn hàng</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _orderService.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo đơn hàng mới (tự động publish Kafka message)
        /// </summary>
        /// <param name="dto">Thông tin đơn hàng</param>
        /// <returns>Đơn hàng vừa tạo</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
        {
            var result = await _orderService.CreateAsync(dto);
            return Created($"/api/orders/{result.order_id}", result);
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng
        /// </summary>
        /// <param name="id">ID đơn hàng</param>
        /// <param name="status">Trạng thái mới</param>
        /// <returns>Đơn hàng đã cập nhật</returns>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
        {
            var result = await _orderService.UpdateStatusAsync(id, dto.status);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật đơn hàng đầy đủ
        /// </summary>
        /// <param name="id">ID đơn hàng</param>
        /// <param name="dto">Thông tin đơn hàng</param>
        /// <returns>Đơn hàng đã cập nhật</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] OrderCreateDto dto)
        {
            var result = await _orderService.UpdateAsync(id, dto);
            return Ok(result);
        }
    }

    /// <summary>
    /// DTO cập nhật trạng thái
    /// </summary>
    public class UpdateStatusDto
    {
        /// <summary>
        /// Trạng thái mới
        /// </summary>
        public string status { get; set; }
    }
}
