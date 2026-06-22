using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces.Stock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BusinessApi.Controllers
{
    /// <summary>
    /// Controller quản lý kho
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StocksController : ControllerBase
    {
        private readonly IStockService _stockService;

        /// <summary>
        /// Khởi tạo StocksController
        /// </summary>
        /// <param name="stockService">Service kho</param>
        public StocksController(IStockService stockService)
        {
            _stockService = stockService;
        }

        /// <summary>
        /// Lấy tất cả kho
        /// </summary>
        /// <returns>Danh sách kho</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _stockService.GetAllAsync();
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
            var result = await _stockService.GetAllPagingAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy kho theo ID
        /// </summary>
        /// <param name="id">ID kho</param>
        /// <returns>Kho</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _stockService.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo kho mới
        /// </summary>
        /// <param name="dto">Thông tin kho</param>
        /// <returns>Kho vừa tạo</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockCreateDto dto)
        {
            var result = await _stockService.CreateAsync(dto);
            return Created($"/api/stocks/{result.stock_id}", result);
        }

        /// <summary>
        /// Cập nhật kho
        /// </summary>
        /// <param name="id">ID kho</param>
        /// <param name="dto">Thông tin kho</param>
        /// <returns>Kho đã cập nhật</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] StockCreateDto dto)
        {
            var result = await _stockService.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa kho
        /// </summary>
        /// <param name="id">ID kho</param>
        /// <returns>Kết quả xóa</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _stockService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
