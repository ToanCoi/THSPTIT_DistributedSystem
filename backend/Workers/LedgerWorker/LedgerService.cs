using BE.Application.Contracts.Interfaces.Ledger;
using BE.Domain.DI.Ledger;
using BE.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Workers.LedgerWorker;

/// <summary>
/// Service xử lý nghiệp vụ sổ cái tồn kho - ghi ledger khi có phiếu nhập/xuất kho
/// </summary>
public class LedgerService : ILedgerService
{
    private readonly ILedgerRepo _ledgerRepo;
    private readonly ILogger<LedgerService> _logger;

    public LedgerService(ILedgerRepo ledgerRepo, ILogger<LedgerService> logger)
    {
        _ledgerRepo = ledgerRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ProcessInwardAsync(Guid inwardId, Guid productId, Guid stockId, decimal quantity)
    {
        _logger.LogInformation("Processing inward: inward={inward_id}, product={product_id}, qty={quantity}",
            inwardId, productId, quantity);

        var now = DateTime.UtcNow;

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
            created_by = "LedgerWorker"
        };

        await _ledgerRepo.InsertAsync(ledger);
        await _ledgerRepo.UpsertLedgerDateAsync(productId, stockId, quantity, 0, now);
        await _ledgerRepo.UpsertClosingAsync(productId, stockId, quantity);

        _logger.LogInformation("Processed inward {inward_id}", inwardId);
    }

    /// <inheritdoc />
    public async Task ProcessOutwardAsync(Guid outwardId, Guid productId, Guid stockId, decimal quantity)
    {
        _logger.LogInformation("Processing outward: outward={outward_id}, product={product_id}, qty={quantity}",
            outwardId, productId, quantity);

        var now = DateTime.UtcNow;

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
            created_by = "LedgerWorker"
        };

        await _ledgerRepo.InsertAsync(ledger);
        await _ledgerRepo.UpsertLedgerDateAsync(productId, stockId, 0, quantity, now);
        await _ledgerRepo.UpsertClosingAsync(productId, stockId, -quantity);

        _logger.LogInformation("Processed outward {outward_id}", outwardId);
    }

    /// <inheritdoc />
    public async Task ProcessOrderItemAsync(Guid orderId, Guid productId, Guid stockId, decimal quantity, decimal unitPrice)
    {
        // Tạo outward tương ứng rồi xử lý ledger
        var outwardId = Guid.NewGuid();
        _logger.LogInformation("Processing order item: order={order_id}, product={product_id}, stock={stock_id}, qty={quantity}",
            orderId, productId, stockId, quantity);

        await ProcessOutwardAsync(outwardId, productId, stockId, quantity);

        _logger.LogInformation("Processed order item {order_id}/{product_id} -> outward {outward_id}",
            orderId, productId, outwardId);
    }
}
