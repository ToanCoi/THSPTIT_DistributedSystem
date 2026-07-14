using BE.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.DI.Ledger
{
    /// <summary>
    /// Interface repository sổ cái tồn kho
    /// </summary>
    public interface ILedgerRepo
    {
        /// <summary>
        /// Thêm bản ghi sổ cái
        /// </summary>
        Task<bool> InsertAsync(LedgerEntity ledger);

        /// <summary>
        /// Cập nhật sổ cái theo ngày (upsert)
        /// </summary>
        Task<bool> UpsertLedgerDateAsync(Guid productId, Guid stockId, decimal inwardQty, decimal outwardQty, DateTime ledgerDate);

        /// <summary>
        /// Cập nhật số dư đóng (closing)
        /// </summary>
        Task<bool> UpsertClosingAsync(Guid productId, Guid stockId, decimal quantity);

        /// <summary>
        /// Lấy số lượng tồn kho của sản phẩm theo kho
        /// </summary>
        Task<decimal> GetClosingQuantityAsync(Guid productId, Guid stockId);

        /// <summary>
        /// Lấy tất cả bản ghi ledger theo reference_id (voucher)
        /// </summary>
        Task<IEnumerable<LedgerEntity>> GetByReferenceIdAsync(Guid referenceId);

        /// <summary>
        /// Xóa tất cả bản ghi ledger theo reference_id (voucher)
        /// </summary>
        Task<int> DeleteByReferenceIdAsync(Guid referenceId);
    }
}