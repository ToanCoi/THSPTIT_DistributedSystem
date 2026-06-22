using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces.Inward;
using BE.Application.Exceptions;
using BE.Domain.DI.Inward;
using BE.Domain.Entities;
using BE.Domain.Repos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Workers.Shared.Models;
using Workers.Shared.Services;

namespace BE.Application.Services.Inward
{
    /// <summary>
    /// Service xử lý nghiệp vụ phiếu nhập kho
    /// </summary>
    public class InwardService : IInwardService
    {
        private readonly IInwardRepo _inwardRepo;
        private readonly IBaseRepo _baseRepo;
        private readonly ILogger<InwardService> _logger;
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly string _ledgerTopic;

        public InwardService(
            IInwardRepo inwardRepo,
            IBaseRepo baseRepo,
            ILogger<InwardService> logger,
            IKafkaProducerService kafkaProducer,
            IConfiguration configuration)
        {
            _inwardRepo = inwardRepo;
            _baseRepo = baseRepo;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _ledgerTopic = configuration["Kafka:LedgerTopic"] ?? "ledger-change";
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
        public async Task<PagingResult<InwardDto>> GetAllPagingAsync(PagingFilterDto filter)
        {
            var columns = "inward_id, product_id, stock_id, quantity, unit_price, supplier, invoice_date, created_date";
            var sort = $"{filter.sort_field} {filter.sort_order}";

            var pagingResult = await _baseRepo.GetPaging<InwardEntity>(
                columns,
                filter.skip,
                filter.take,
                sort,
                null
            );

            var dtos = new List<InwardDto>();
            if (pagingResult.Data != null)
            {
                foreach (var entity in pagingResult.Data.Cast<InwardEntity>())
                {
                    dtos.Add(MapToDto(entity));
                }
            }

            return new PagingResult<InwardDto>
            {
                data = dtos,
                total = dtos.Count
            };
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
                selling_price = dto.selling_price,
                supplier = dto.supplier,
                invoice_date = dto.invoice_date,
                created_date = DateTime.UtcNow,
                created_by = "system"
            };

            await _inwardRepo.InsertAsync(inward);

            var ledgerMsg = new LedgerChangeMessage
            {
                voucher_id = inward.inward_id.ToString(),
                voucher_type = "INWARD",
                product_id = inward.product_id.ToString(),
                stock_id = inward.stock_id.ToString(),
                quantity = inward.quantity,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            var json = System.Text.Json.JsonSerializer.Serialize(ledgerMsg);
            await _kafkaProducer.ProduceAsync(_ledgerTopic, inward.inward_id.ToString(), json);

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
