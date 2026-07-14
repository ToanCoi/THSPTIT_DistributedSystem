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

        /// <inheritdoc />
        public async Task<OutwardDto> UpdateAsync(Guid outwardId, OutwardUpdateDto dto)
        {
            var existing = await _outwardRepo.GetByIdAsync(outwardId);
            if (existing == null)
            {
                throw new BusinessException("Không tìm thấy phiếu xuất", 404);
            }

            // RULE: Phiếu xuất có gắn với đơn hàng (do VoucherWorker tạo) thì không cho sửa
            if (existing.order_id.HasValue && existing.order_id.Value != Guid.Empty)
            {
                throw new BusinessException(
                    "Không thể sửa phiếu xuất gắn với đơn hàng. Phiếu này do VoucherWorker tạo tự động từ đơn hàng.",
                    422
                );
            }

            // Lưu giá trị cũ để tính delta cho ledger
            var oldProductId = existing.product_id;
            var oldStockId = existing.stock_id;
            var oldQuantity = existing.quantity;

            // Cập nhật entity
            existing.product_id = dto.product_id;
            existing.stock_id = dto.stock_id;
            existing.quantity = dto.quantity;
            existing.unit_price = dto.unit_price;
            existing.outward_date = dto.outward_date;

            await _outwardRepo.UpdateAsync(existing);

            // Publish ledger-change với event_type=UPDATE để LedgerWorker tính delta
            var ledgerMsg = new LedgerChangeMessage
            {
                voucher_id = existing.outward_id.ToString(),
                voucher_type = "OUTWARD",
                product_id = existing.product_id.ToString(),
                stock_id = existing.stock_id.ToString(),
                quantity = existing.quantity,
                timestamp = DateTime.UtcNow.ToString("o"),
                event_type = "UPDATE",
                old_quantity = oldQuantity,
                old_product_id = oldProductId.ToString(),
                old_stock_id = oldStockId.ToString()
            };
            var json = System.Text.Json.JsonSerializer.Serialize(ledgerMsg);
            await _kafkaProducer.ProduceAsync(_ledgerTopic, existing.outward_id.ToString(), json);

            _logger.LogInformation("Cập nhật phiếu xuất [{outward_id}]", existing.outward_id);
            return MapToDto(existing);
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(Guid outwardId)
        {
            var existing = await _outwardRepo.GetByIdAsync(outwardId);
            if (existing == null)
            {
                throw new BusinessException("Không tìm thấy phiếu xuất", 404);
            }

            // RULE: phiếu xuất do VoucherWorker tạo tự động từ đơn hàng không thể xóa trực tiếp
            if (existing.order_id.HasValue && existing.order_id.Value != Guid.Empty)
            {
                throw new BusinessException(
                    "Không thể xóa phiếu xuất gắn với đơn hàng. Hãy xóa đơn hàng để cascade.",
                    422);
            }

            // Publish ledger UPDATE với quantity=0 để LedgerWorker reverse entries cũ
            var ledgerMsg = new LedgerChangeMessage
            {
                voucher_id = existing.outward_id.ToString(),
                voucher_type = "OUTWARD",
                product_id = existing.product_id.ToString(),
                stock_id = existing.stock_id.ToString(),
                quantity = 0,
                timestamp = DateTime.UtcNow.ToString("o"),
                event_type = "UPDATE",
                old_quantity = existing.quantity,
                old_product_id = existing.product_id.ToString(),
                old_stock_id = existing.stock_id.ToString()
            };
            var json = System.Text.Json.JsonSerializer.Serialize(ledgerMsg);
            await _kafkaProducer.ProduceAsync(_ledgerTopic,
                existing.outward_id.ToString(), json);

            _logger.LogInformation("Xóa phiếu xuất [{outward_id}]", existing.outward_id);
            return await _outwardRepo.DeleteAsync(outwardId);
        }

        private OutwardDto MapToDto(OutwardEntity outward)
        {
            return new OutwardDto
            {
                outward_id = outward.outward_id,
                order_id = outward.order_id,
                order_code = outward.order_code,
                product_id = outward.product_id,
                product_name = outward.product_name,
                stock_id = outward.stock_id,
                stock_name = outward.stock_name,
                quantity = outward.quantity,
                unit_price = outward.unit_price,
                outward_date = outward.outward_date,
                created_date = outward.created_date
            };
        }
    }
}