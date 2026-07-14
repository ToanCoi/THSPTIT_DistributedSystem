using BE.Application.Contracts.Interfaces.Ledger;
using BE.Domain.DI.Ledger;
using BE.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <inheritdoc />
    /// <summary>
    /// Xử lý cập nhật phiếu nhập/xuất:
    /// 1. Lấy tất cả entry ledger cũ theo reference_id
    /// 2. Reverse impact (closing + ledger_date) của entries cũ - tính toán closing closing mới
    /// 3. Xóa entries cũ trong sổ
    /// 4. Insert entry mới với quantity mới
    /// 5. Apply impact mới lên closing và ledger_date
    /// Xử lý được mọi case: cùng stock/đổi stock, tăng/giảm quantity.
    /// </summary>
    public async Task ProcessUpdateAsync(
        Guid voucherId,
        string voucherType,
        Guid newProductId,
        Guid newStockId,
        decimal newQuantity,
        decimal oldQuantity,
        Guid oldProductId,
        Guid oldStockId)
    {
        _logger.LogInformation(
            "Processing update (rebuild): voucher={voucher_id}, type={voucher_type}, newQty={newQty}, oldQty={oldQty}, newStock={newStock}, oldStock={oldStock}",
            voucherId, voucherType, newQuantity, oldQuantity, newStockId, oldStockId);

        // 1. Lấy tất cả entries cũ theo reference_id
        var oldEntries = (await _ledgerRepo.GetByReferenceIdAsync(voucherId)).ToList();
        _logger.LogInformation("Found {count} old ledger entries for voucher {voucher_id}",
            oldEntries.Count, voucherId);

        // 2. Reverse impact cũ lên closing + ledger_date (cho từng entry)
        foreach (var old in oldEntries)
        {
            if (old.product_id == Guid.Empty || old.stock_id == Guid.Empty)
            {
                continue;
            }

            // closing gốc đã apply: +inward - outward
            // để reverse: cần apply -inward + outward, tức là net = outward - inward
            decimal reverseDelta = old.outward_quantity - old.inward_quantity;
            if (reverseDelta != 0)
            {
                await _ledgerRepo.UpsertClosingAsync(old.product_id, old.stock_id, reverseDelta);
            }

            if (old.inward_quantity != 0 || old.outward_quantity != 0)
            {
                await _ledgerRepo.UpsertLedgerDateAsync(
                    old.product_id, old.stock_id,
                    -old.inward_quantity,
                    -old.outward_quantity,
                    old.ledger_date);
            }
        }

        // 3. Xóa entries cũ trong sổ
        if (oldEntries.Count > 0)
        {
            await _ledgerRepo.DeleteByReferenceIdAsync(voucherId);
            _logger.LogInformation("Deleted {count} old ledger entries for voucher {voucher_id}",
                oldEntries.Count, voucherId);
        }

        // 4. Nếu quantity mới = 0 thì không insert entry mới
        if (newQuantity == 0)
        {
            _logger.LogInformation("New quantity = 0, skip insert. Voucher {voucher_id} now has no ledger entry.",
                voucherId);
            return;
        }

        // 5. Insert entry mới
        var now = DateTime.UtcNow;
        decimal inwardQty = 0;
        decimal outwardQty = 0;
        decimal closingDelta = 0;

        bool isInward = voucherType.Equals("INWARD", StringComparison.OrdinalIgnoreCase);
        if (isInward)
        {
            inwardQty = newQuantity;
            closingDelta = newQuantity;
        }
        else
        {
            outwardQty = newQuantity;
            closingDelta = -newQuantity;
        }

        var ledger = new LedgerEntity
        {
            ledger_id = Guid.NewGuid(),
            product_id = newProductId,
            stock_id = newStockId,
            inward_quantity = inwardQty,
            outward_quantity = outwardQty,
            reference_id = voucherId,
            reference_type = voucherType,
            ledger_date = now,
            created_date = now,
            created_by = "LedgerWorker"
        };

        await _ledgerRepo.InsertAsync(ledger);
        await _ledgerRepo.UpsertLedgerDateAsync(newProductId, newStockId, inwardQty, outwardQty, now);
        await _ledgerRepo.UpsertClosingAsync(newProductId, newStockId, closingDelta);

        _logger.LogInformation(
            "Inserted new ledger entry for voucher {voucher_id}: product={product_id}, stock={stock_id}, qty={qty}, type={voucher_type}",
            voucherId, newProductId, newStockId, newQuantity, voucherType);
    }
}