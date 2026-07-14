using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces.Order;
using BE.Application.Exceptions;
using Confluent.Kafka;
using BE.Domain.DI.Order;
using BE.Domain.DI.Outward;
using BE.Domain.Entities;
using BE.Domain.Repos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Workers.Shared.Models;
using Workers.Shared.Services;

namespace BE.Application.Services.Order
{
    /// <summary>
    /// Service xử lý nghiệp vụ đơn hàng và Kafka producer
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IOrderRepo _orderRepo;
        private readonly IOrderItemRepo _orderItemRepo;
        private readonly IBaseRepo _baseRepo;
        private readonly ILogger<OrderService> _logger;
        private readonly IProducer<string, string> _kafkaProducer;
        private readonly string _kafkaTopic;

        private readonly IConfiguration _configuration;

        private readonly IOutwardRepo _outwardRepo;
        private readonly IKafkaProducerService _ledgerProducer;
        private readonly string _ledgerTopic;

        public OrderService(
            IOrderRepo orderRepo,
            IOrderItemRepo orderItemRepo,
            IBaseRepo baseRepo,
            ILogger<OrderService> logger,
            IConfiguration configuration,
            IOutwardRepo outwardRepo,
            IKafkaProducerService ledgerProducer)
        {
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _baseRepo = baseRepo;
            _logger = logger;
            _configuration = configuration;
            _outwardRepo = outwardRepo;
            _ledgerProducer = ledgerProducer;

            // Cấu hình Kafka producer từ config
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9093",
                Acks = Acks.All,
                SocketTimeoutMs = 5000,
                MessageTimeoutMs = 5000
            };
            _kafkaProducer = new ProducerBuilder<string, string>(producerConfig).Build();
            _kafkaTopic = _configuration["Kafka:Topic"] ?? "order-created";
            _ledgerTopic = _configuration["Kafka:LedgerTopic"] ?? "ledger-change";
        }

        /// <inheritdoc />
        public async Task<OrderDto> GetByIdAsync(Guid orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException("Không tìm thấy đơn hàng", 404);
            }

            var items = await _orderItemRepo.GetByOrderIdAsync(orderId);
            return MapToDto(order, items);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrderDto>> GetAllAsync()
        {
            var orders = await _orderRepo.GetAllAsync();
            var result = new List<OrderDto>();
            foreach (var order in orders)
            {
                var items = await _orderItemRepo.GetByOrderIdAsync(order.order_id);
                result.Add(MapToDto(order, items));
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<PagingResult<OrderDto>> GetAllPagingAsync(PagingFilterDto filter)
        {
            var columns = "order_id, customer_id, stock_id, order_code, total_amount, status, order_date, created_date";
            var sort = $"{filter.sort_field} {filter.sort_order}";

            var pagingResult = await _baseRepo.GetPaging<OrderEntity>(
                columns,
                filter.skip,
                filter.take,
                sort,
                null
            );

            var dtos = new List<OrderDto>();
            if (pagingResult.Data != null)
            {
                foreach (var entity in pagingResult.Data.Cast<OrderEntity>())
                {
                    var items = await _orderItemRepo.GetByOrderIdAsync(entity.order_id);
                    dtos.Add(MapToDto(entity, items));
                }
            }

            return new PagingResult<OrderDto>
            {
                data = dtos,
                total = dtos.Count
            };
        }

        /// <inheritdoc />
        public async Task<OrderDto> CreateAsync(OrderCreateDto dto)
        {
            // Tạo đơn hàng
            var order = new OrderEntity
            {
                order_id = Guid.NewGuid(),
                customer_id = dto.customer_id,
                stock_id = dto.stock_id,
                order_code = await GenerateOrderCodeAsync(),
                total_amount = 0,
                status = "PENDING",
                order_date = DateTime.UtcNow,
                created_date = DateTime.UtcNow,
                created_by = "system"
            };

            // Tính tổng tiền
            decimal totalAmount = 0;
            var orderItems = new List<OrderItemEntity>();

            foreach (var itemDto in dto.items)
            {
                var orderItem = new OrderItemEntity
                {
                    order_item_id = Guid.NewGuid(),
                    order_id = order.order_id,
                    product_id = itemDto.product_id,
                    quantity = itemDto.quantity,
                    unit_price = itemDto.unit_price,
                    created_date = DateTime.UtcNow,
                    created_by = "system"
                };
                orderItems.Add(orderItem);
                totalAmount += itemDto.quantity * itemDto.unit_price;
            }
            order.total_amount = totalAmount;

            // Lưu đơn hàng
            await _orderRepo.InsertAsync(order);

            // Lưu chi tiết đơn hàng
            await _orderItemRepo.InsertManyAsync(orderItems);

            // Publish Kafka message
            await PublishOrderCreatedMessage(order, dto.stock_id, dto.items);

            _logger.LogInformation("Tạo đơn hàng mới [{order_id}]", order.order_id);

            return MapToDto(order, orderItems);
        }

        /// <inheritdoc />
        public async Task<OrderDto> UpdateStatusAsync(Guid orderId, string status)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException("Không tìm thấy đơn hàng", 404);
            }

            order.status = status;
            await _orderRepo.UpdateAsync(order);

            _logger.LogInformation("Cập nhật trạng thái đơn hàng [{order_id}] -> {status}", orderId, status);

            var items = await _orderItemRepo.GetByOrderIdAsync(orderId);
            return MapToDto(order, items);
        }

        /// <inheritdoc />
        public async Task<OrderDto> UpdateAsync(Guid orderId, OrderCreateDto dto)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException("Không tìm thấy đơn hàng", 404);
            }

            // Cập nhật thông tin đơn hàng
            order.customer_id = dto.customer_id;
            order.stock_id = dto.stock_id;
            order.order_date = dto.order_date;

            // Tính lại tổng tiền
            decimal totalAmount = 0;
            var orderItems = new List<OrderItemEntity>();

            foreach (var itemDto in dto.items)
            {
                var orderItem = new OrderItemEntity
                {
                    order_item_id = Guid.NewGuid(),
                    order_id = order.order_id,
                    product_id = itemDto.product_id,
                    quantity = itemDto.quantity,
                    unit_price = itemDto.unit_price,
                    created_date = DateTime.UtcNow,
                    created_by = "system"
                };
                orderItems.Add(orderItem);
                totalAmount += itemDto.quantity * itemDto.unit_price;
            }
            order.total_amount = totalAmount;

            // Xóa chi tiết cũ và thêm chi tiết mới
            await _orderItemRepo.DeleteByOrderIdAsync(orderId);
            await _orderItemRepo.InsertManyAsync(orderItems);

            // Cập nhật đơn hàng
            await _orderRepo.UpdateAsync(order);

            _logger.LogInformation("Cập nhật đơn hàng [{order_id}]", orderId);

            return MapToDto(order, orderItems);
        }

        /// <summary>
        /// Publish message to Kafka topic
        /// </summary>
        private async Task PublishOrderCreatedMessage(OrderEntity order, Guid stockId, List<OrderItemCreateDto> items)
        {
            var message = new
            {
                order_id = order.order_id.ToString(),
                customer_id = order.customer_id.ToString(),
                stock_id = stockId.ToString(),
                order_code = order.order_code,
                items = items.Select(i => new
                {
                    product_id = i.product_id.ToString(),
                    quantity = i.quantity,
                    unit_price = i.unit_price
                }).ToList(),
                timestamp = DateTime.UtcNow.ToString("o")
            };

            var jsonMessage = System.Text.Json.JsonSerializer.Serialize(message);

            await _kafkaProducer.ProduceAsync(_kafkaTopic, new Message<string, string>
            {
                Key = order.order_id.ToString(),
                Value = jsonMessage
            });

            _logger.LogInformation("Published message to Kafka topic [{topic}] for order [{order_id}]", _kafkaTopic, order.order_id);
        }

        /// <summary>
        /// Tạo mã đơn hàng tự động (DH + sequence int, atomic với row lock)
        /// </summary>
        private async Task<string> GenerateOrderCodeAsync()
        {
            var next = await _orderRepo.GetNextOrderCodeAsync();
            return $"DH{next}";
        }

        private OrderDto MapToDto(OrderEntity order, IEnumerable<OrderItemEntity> items)
        {
            return new OrderDto
            {
                order_id = order.order_id,
                customer_id = order.customer_id,
                customer_name = order.customer_name,
                stock_id = order.stock_id,
                stock_name = order.stock_name,
                order_code = order.order_code,
                total_amount = order.total_amount,
                status = order.status,
                order_date = order.order_date,
                created_date = order.created_date,
                items = items.Select(i => new OrderItemDto
                {
                    order_item_id = i.order_item_id,
                    product_id = i.product_id,
                    quantity = i.quantity,
                    unit_price = i.unit_price
                }).ToList()
            };
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(Guid orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException("Không tìm thấy đơn hàng", 404);
            }

            // 1. Publish ledger UPDATE (quantity=0) cho từng phiếu xuất gắn với đơn hàng
            //    để LedgerWorker reverse impact cũ (ProcessUpdateAsync xử lý newQuantity=0)
            var outwards = (await _outwardRepo.GetByOrderIdAsync(orderId)).ToList();
            foreach (var outward in outwards)
            {
                var ledgerMsg = new LedgerChangeMessage
                {
                    voucher_id = outward.outward_id.ToString(),
                    voucher_type = "OUTWARD",
                    product_id = outward.product_id.ToString(),
                    stock_id = outward.stock_id.ToString(),
                    quantity = 0,
                    timestamp = DateTime.UtcNow.ToString("o"),
                    event_type = "UPDATE",
                    old_quantity = outward.quantity,
                    old_product_id = outward.product_id.ToString(),
                    old_stock_id = outward.stock_id.ToString()
                };
                var json = System.Text.Json.JsonSerializer.Serialize(ledgerMsg);
                await _ledgerProducer.ProduceAsync(_ledgerTopic,
                    outward.outward_id.ToString(), json);
            }

            // 2. Xóa phiếu xuất, chi tiết đơn, đơn hàng
            await _outwardRepo.DeleteByOrderIdAsync(orderId);
            await _orderItemRepo.DeleteByOrderIdAsync(orderId);
            return await _orderRepo.DeleteAsync(orderId);
        }
    }
}
