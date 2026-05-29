using BE.Application.Contracts.Interfaces.Inward;
using BE.Application.Exceptions;
using BE.Domain.DI.Inward;
using BE.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Services.Inward
{
    /// <summary>
    /// Service xử lý nghiệp vụ phiếu nhập kho
    /// </summary>
    public class InwardService : IInwardService
    {
        private readonly IInwardRepo _inwardRepo;
        private readonly ILogger<InwardService> _logger;

        public InwardService(IInwardRepo inwardRepo, ILogger<InwardService> logger)
        {
            _inwardRepo = inwardRepo;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<InwardDto> GetByIdAsync(Guid inwardId)
        {
            var inward = await _inwardRepo.GetByIdAsync(inwardId);
            if (inward == null)
            {
                throw new BusinessException("Không tìm thấy phiếu nhập", 404);
            }
            return MapToDto(inward);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<InwardDto>> GetAllAsync()
        {
            var inwards = await _inwardRepo.GetAllAsync();
            var result = new List<InwardDto>();
            foreach (var inward in inwards)
            {
                result.Add(MapToDto(inward));
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<InwardDto> CreateAsync(InwardCreateDto dto)
        {
            var inward = new InwardEntity
            {
                inward_id = Guid.NewGuid(),
                product_id = dto.product_id,
                stock_id = dto.stock_id,
                quantity = dto.quantity,
                unit_price = dto.unit_price,
                supplier = dto.supplier,
                invoice_date = dto.invoice_date,
                created_date = DateTime.UtcNow,
                created_by = "system"
            };

            await _inwardRepo.InsertAsync(inward);
            _logger.LogInformation("Tạo phiếu nhập mới [{inward_id}]", inward.inward_id);
            return MapToDto(inward);
        }

        private InwardDto MapToDto(InwardEntity inward)
        {
            return new InwardDto
            {
                inward_id = inward.inward_id,
                product_id = inward.product_id,
                stock_id = inward.stock_id,
                quantity = inward.quantity,
                unit_price = inward.unit_price,
                supplier = inward.supplier,
                invoice_date = inward.invoice_date,
                created_date = inward.created_date
            };
        }
    }
}