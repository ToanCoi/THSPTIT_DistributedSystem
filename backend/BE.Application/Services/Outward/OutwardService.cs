using BE.Application.Contracts.Interfaces.Outward;
using BE.Application.Exceptions;
using BE.Domain.DI.Outward;
using BE.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Services.Outward
{
    /// <summary>
    /// Service xử lý nghiệp vụ phiếu xuất kho
    /// </summary>
    public class OutwardService : IOutwardService
    {
        private readonly IOutwardRepo _outwardRepo;
        private readonly ILogger<OutwardService> _logger;

        public OutwardService(IOutwardRepo outwardRepo, ILogger<OutwardService> logger)
        {
            _outwardRepo = outwardRepo;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<OutwardDto> GetByIdAsync(Guid outwardId)
        {
            var outward = await _outwardRepo.GetByIdAsync(outwardId);
            if (outward == null)
            {
                throw new BusinessException("Không tìm thấy phiếu xuất", 404);
            }
            return MapToDto(outward);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OutwardDto>> GetAllAsync()
        {
            var outwards = await _outwardRepo.GetAllAsync();
            var result = new List<OutwardDto>();
            foreach (var outward in outwards)
            {
                result.Add(MapToDto(outward));
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<OutwardDto> CreateAsync(OutwardCreateDto dto)
        {
            var outward = new OutwardEntity
            {
                outward_id = Guid.NewGuid(),
                order_id = dto.order_id,
                product_id = dto.product_id,
                stock_id = dto.stock_id,
                quantity = dto.quantity,
                unit_price = dto.unit_price,
                outward_date = dto.outward_date,
                created_date = DateTime.UtcNow,
                created_by = "system"
            };

            await _outwardRepo.InsertAsync(outward);
            _logger.LogInformation("Tạo phiếu xuất mới [{outward_id}]", outward.outward_id);
            return MapToDto(outward);
        }

        private OutwardDto MapToDto(OutwardEntity outward)
        {
            return new OutwardDto
            {
                outward_id = outward.outward_id,
                order_id = outward.order_id,
                product_id = outward.product_id,
                stock_id = outward.stock_id,
                quantity = outward.quantity,
                unit_price = outward.unit_price,
                outward_date = outward.outward_date,
                created_date = outward.created_date
            };
        }
    }
}