using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BusinessApi.Controllers
{
    /// <summary>
    /// Controller quản lý sản phẩm
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        /// <summary>
        /// Khởi tạo ProductsController
        /// </summary>
        /// <param name="productService">Service sản phẩm</param>
        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Lấy tất cả sản phẩm
        /// </summary>
        /// <returns>Danh sách sản phẩm</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _productService.GetAllAsync();
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
            var result = await _productService.GetAllPagingAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy sản phẩm theo ID
        /// </summary>
        /// <param name="id">ID sản phẩm</param>
        /// <returns>Sản phẩm</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _productService.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo sản phẩm mới
        /// </summary>
        /// <param name="dto">Thông tin sản phẩm</param>
        /// <returns>Sản phẩm vừa tạo</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            var result = await _productService.CreateAsync(dto);
            return Created($"/api/products/{result.product_id}", result);
        }

        /// <summary>
        /// Cập nhật sản phẩm
        /// </summary>
        /// <param name="id">ID sản phẩm</param>
        /// <param name="dto">Thông tin cập nhật</param>
        /// <returns>Sản phẩm đã cập nhật</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProductUpdateDto dto)
        {
            var result = await _productService.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa sản phẩm
        /// </summary>
        /// <param name="id">ID sản phẩm</param>
        /// <returns>Không có nội dung</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
    }
}
