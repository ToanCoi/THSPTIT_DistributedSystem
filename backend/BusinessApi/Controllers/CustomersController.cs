using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BusinessApi.Controllers
{
    /// <summary>
    /// Controller quản lý khách hàng
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        /// <summary>
        /// Khởi tạo CustomersController
        /// </summary>
        /// <param name="customerService">Service khách hàng</param>
        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// Lấy tất cả khách hàng
        /// </summary>
        /// <returns>Danh sách khách hàng</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _customerService.GetAllAsync();
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
            var result = await _customerService.GetAllPagingAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy khách hàng theo ID
        /// </summary>
        /// <param name="id">ID khách hàng</param>
        /// <returns>Khách hàng</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _customerService.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo khách hàng mới
        /// </summary>
        /// <param name="dto">Thông tin khách hàng</param>
        /// <returns>Khách hàng vừa tạo</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
        {
            var result = await _customerService.CreateAsync(dto);
            return Created($"/api/customers/{result.customer_id}", result);
        }

        /// <summary>
        /// Cập nhật khách hàng
        /// </summary>
        /// <param name="id">ID khách hàng</param>
        /// <param name="dto">Thông tin cập nhật</param>
        /// <returns>Khách hàng đã cập nhật</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CustomerUpdateDto dto)
        {
            var result = await _customerService.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        /// <param name="id">ID khách hàng</param>
        /// <returns>Không có nội dung</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _customerService.DeleteAsync(id);
            return NoContent();
        }
    }
}
