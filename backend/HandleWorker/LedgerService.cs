using BE.Application.Contracts.Interfaces.Ledger;
using BE.Domain.DI.Ledger;
using BE.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HandleWorker
{
    /// <summary>
    /// Service xử lý nghiệp vụ sổ cái tồn kho - ghi ledger khi có đơn hàng
    /// </summary>
    public class LedgerService : ILedgerService
    {
        private readonly ILedgerRepo _ledgerRepo;
        private readonly ILogger<LedgerService> _logger;

        // Mặc định stock_id = Kho chính (sẽ lấy từ config hoặc DB)
        private readonly Guid _defaultStockId = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-345678901234");

        public LedgerService(ILedgerRepo ledgerRepo, ILogger<LedgerService> logger)
        {
            _ledgerRepo = ledgerRepo;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ProcessOrderItemAsync(Guid orderId, Guid productId, decimal quantity, decimal unitPrice)
        {
            _logger.LogInformation("Processing order item: order={order_id}, product={product_id}, qty={quantity}",
                orderId, productId, quantity);

            var now = DateTime.UtcNow;

            // 1. Insert vào led_inventory_item_ledger
            var ledger = new LedgerEntity
            {
                ledger_id = Guid.NewGuid(),
                product_id = productId,
                stock_id = _defaultStockId,
                inward_quantity = 0,
                outward_quantity = quantity,
                reference_id = orderId,
                reference_type = "ORDER",
                ledger_date = now,
                created_date = now,
                created_by = "HandleWorker"
            };

            await _ledgerRepo.InsertAsync(ledger);
            _logger.LogInformation("Inserted ledger record for order {order_id}", orderId);

            // 2. Upsert vào led_inventory_item_ledger_date (cập nhật tồn kho theo ngày)
            await _ledgerRepo.UpsertLedgerDateAsync(productId, _defaultStockId, 0, quantity, now);
            _logger.LogInformation("Updated ledger date for product {product_id}", productId);

            // 3. Upsert vào led_inventory_item_ledger_closing (cập nhật số dư đóng)
            // Số dư đóng = tổng nhập - tổng xuất (cần tính toán thực tế trong production)
            // Trong demo này, giảm quantity vì là xuất kho
            var closingQty = -quantity; // Sẽ cần tính toán chính xác hơn trong production
            await _ledgerRepo.UpsertClosingAsync(productId, _defaultStockId, closingQty);
            _logger.LogInformation("Updated closing balance for product {product_id}", productId);
        }

        /// <inheritdoc />
        public async Task ProcessInwardAsync(Guid inwardId, Guid productId, Guid stockId, decimal quantity, decimal unitPrice)
        {
            _logger.LogInformation("Processing inward: inward={inward_id}, product={product_id}, qty={quantity}",
                inwardId, productId, quantity);

            var now = DateTime.UtcNow;

            // Insert vào led_inventory_item_ledger
            var ledger = new LedgerEntity
            {
                ledger_id = Guid.NewGuid(),
                product_id = productId,
                stock_id = stockId,
                inward_quantity = quantity,
                outward_quantity = 0,
                reference_id = inwardId,
                reference_type = "INWARD",
                ledger_date = now,
                created_date = now,
                created_by = "HandleWorker"
            };

            await _ledgerRepo.InsertAsync(ledger);

            // Upsert ledger date
            await _ledgerRepo.UpsertLedgerDateAsync(productId, stockId, quantity, 0, now);

            // Upsert closing (tăng tồn kho)
            var closingQty = quantity;
            await _ledgerRepo.UpsertClosingAsync(productId, stockId, closingQty);

            _logger.LogInformation("Processed inward {inward_id}", inwardId);
        }

        /// <inheritdoc />
        public async Task ProcessOutwardAsync(Guid outwardId, Guid productId, Guid stockId, decimal quantity, decimal unitPrice)
        {
            _logger.LogInformation("Processing outward: outward={outward_id}, product={product_id}, qty={quantity}",
                outwardId, productId, quantity);

            var now = DateTime.UtcNow;

            // Insert vào led_inventory_item_ledger
            var ledger = new LedgerEntity
            {
                ledger_id = Guid.NewGuid(),
                product_id = productId,
                stock_id = stockId,
                inward_quantity = 0,
                outward_quantity = quantity,
                reference_id = outwardId,
                reference_type = "OUTWARD",
                ledger_date = now,
                created_date = now,
                created_by = "HandleWorker"
            };

            await _ledgerRepo.InsertAsync(ledger);

            // Upsert ledger date
            await _ledgerRepo.UpsertLedgerDateAsync(productId, stockId, 0, quantity, now);

            // Upsert closing (giảm tồn kho)
            var closingQty = -quantity;
            await _ledgerRepo.UpsertClosingAsync(productId, stockId, closingQty);

            _logger.LogInformation("Processed outward {outward_id}", outwardId);
        }
    }
}