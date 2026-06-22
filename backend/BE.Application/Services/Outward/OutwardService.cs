using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces.Outward;
using BE.Application.Exceptions;
using BE.Domain.DI.Outward;
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

namespace BE.Application.Services.Outward
{
    /// <summary>
    /// Service xử lý nghiệp vụ phiếu xuất kho
    /// </summary>
    public class OutwardService : IOutwardService
    {
        private readonly IOutwardRepo _outwardRepo;
        private readonly IBaseRepo _baseRepo;
        private readonly ILogger<OutwardService> _logger;
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly string _ledgerTopic;

        public OutwardService(
            IOutwardRepo outwardRepo,
            IBaseRepo baseRepo,
            ILogger<OutwardService> logger,
            IKafkaProducerService kafkaProducer,
            IConfiguration configuration)
        {
            _outwardRepo = outwardRepo;
            _baseRepo = baseRepo;
            _logger = logger;
            _kafkaProducer = kafkaProducer;
            _ledgerTopic = configuration["Kafka:LedgerTopic"] ?? "ledger-change";
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
        public async Task<PagingResult<OutwardDto>> GetAllPagingAsync(PagingFilterDto filter)
        {
            var columns = "outward_id, order_id, product_id, stock_id, quantity, unit_price, outward_date, created_date";
            var sort = $"{filter.sort_field} {filter.sort_order}";

            var pagingResult = await _baseRepo.GetPaging<OutwardEntity>(
                columns,
                filter.skip,
                filter.take,
                sort,
                null
            );

            var dtos = new List<OutwardDto>();
            if (pagingResult.Data != null)
            {
                foreach (var entity in pagingResult.Data.Cast<OutwardEntity>())
                {
                    dtos.Add(MapToDto(entity));
                }
            }

            return new PagingResult<OutwardDto>
            {
                data = dtos,
                total = dtos.Count
            };
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

            var ledgerMsg = new LedgerChangeMessage
            {
                voucher_id = outward.outward_id.ToString(),
                voucher_type = "OUTWARD",
                product_id = outward.product_id.ToString(),
                stock_id = outward.stock_id.ToString(),
                quantity = outward.quantity,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            var json = System.Text.Json.JsonSerializer.Serialize(ledgerMsg);
            await _kafkaProducer.ProduceAsync(_ledgerTopic, outward.outward_id.ToString(), json);

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
