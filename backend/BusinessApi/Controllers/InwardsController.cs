using BE.Application.Contracts.Interfaces.Inward;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BusinessApi.Controllers
{
    /// <summary>
    /// Controller quản lý phiếu nhập kho
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InwardsController : ControllerBase
    {
        private readonly IInwardService _inwardService;

        /// <summary>
        /// Khởi tạo InwardsController
        /// </summary>
        /// <param name="inwardService">Service phiếu nhập</param>
        public InwardsController(IInwardService inwardService)
        {
            _inwardService = inwardService;
        }

        /// <summary>
        /// Lấy tất cả phiếu nhập
        /// </summary>
        /// <returns>Danh sách phiếu nhập</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _inwardService.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy phiếu nhập theo ID
        /// </summary>
        /// <param name="id">ID phiếu nhập</param>
        /// <returns>Phiếu nhập</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _inwardService.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo phiếu nhập mới
        /// </summary>
        /// <param name="dto">Thông tin phiếu nhập</param>
        /// <returns>Phiếu nhập vừa tạo</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InwardCreateDto dto)
        {
            var result = await _inwardService.CreateAsync(dto);
            return Created($"/api/inwards/{result.inward_id}", result);
        }
    }
}