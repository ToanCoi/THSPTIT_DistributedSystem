using BE.Application.Contracts.Dtos;
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

        public InwardsController(IInwardService inwardService)
        {
            _inwardService = inwardService;
        }

        /// <summary>
        /// Lấy tất cả phiếu nhập
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _inwardService.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách phân trang
        /// </summary>
        [HttpGet("paging")]
        public async Task<IActionResult> GetAllPaging([FromQuery] PagingFilterDto filter)
        {
            var result = await _inwardService.GetAllPagingAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy phiếu nhập theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _inwardService.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo phiếu nhập mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InwardCreateDto dto)
        {
            var result = await _inwardService.CreateAsync(dto);
            return Created($"/api/inwards/{result.inward_id}", result);
        }

        /// <summary>
        /// Cập nhật phiếu nhập
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] InwardUpdateDto dto)
        {
            var result = await _inwardService.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa phiếu nhập (publish ledger UPDATE quantity=0 để reverse, rồi xóa)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _inwardService.RemoveAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}
