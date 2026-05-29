using BE.Application.Contracts.Interfaces.Outward;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BusinessApi.Controllers
{
    /// <summary>
    /// Controller quản lý phiếu xuất kho
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OutwardsController : ControllerBase
    {
        private readonly IOutwardService _outwardService;

        /// <summary>
        /// Khởi tạo OutwardsController
        /// </summary>
        /// <param name="outwardService">Service phiếu xuất</param>
        public OutwardsController(IOutwardService outwardService)
        {
            _outwardService = outwardService;
        }

        /// <summary>
        /// Lấy tất cả phiếu xuất
        /// </summary>
        /// <returns>Danh sách phiếu xuất</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _outwardService.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy phiếu xuất theo ID
        /// </summary>
        /// <param name="id">ID phiếu xuất</param>
        /// <returns>Phiếu xuất</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _outwardService.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo phiếu xuất mới
        /// </summary>
        /// <param name="dto">Thông tin phiếu xuất</param>
        /// <returns>Phiếu xuất vừa tạo</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OutwardCreateDto dto)
        {
            var result = await _outwardService.CreateAsync(dto);
            return Created($"/api/outwards/{result.outward_id}", result);
        }
    }
}