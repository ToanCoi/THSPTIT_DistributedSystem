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
    }
}