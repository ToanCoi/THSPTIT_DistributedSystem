using BE.Application.Contracts.Dtos;
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

        public OutwardsController(IOutwardService outwardService)
        {
            _outwardService = outwardService;
        }

        /// <summary>
        /// Lấy tất cả phiếu xuất
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _outwardService.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách phân trang
        /// </summary>
        [HttpGet("paging")]
        public async Task<IActionResult> GetAllPaging([FromQuery] PagingFilterDto filter)
        {
            var result = await _outwardService.GetAllPagingAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy phiếu xuất theo ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _outwardService.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Tạo phiếu xuất mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OutwardCreateDto dto)
        {
            var result = await _outwardService.CreateAsync(dto);
            return Created($"/api/outwards/{result.outward_id}", result);
        }

        /// <summary>
        /// Cập nhật phiếu xuất. Phiếu xuất gắn với đơn hàng sẽ bị từ chối (422).
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] OutwardUpdateDto dto)
        {
            var result = await _outwardService.UpdateAsync(id, dto);
            return Ok(result);
        }

        /// <summary>
        /// Xóa phiếu xuất. Phiếu xuất gắn với đơn hàng bị từ chối (422).
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _outwardService.RemoveAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
}
