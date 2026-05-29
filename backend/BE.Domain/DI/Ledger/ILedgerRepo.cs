using BE.Domain.Entities;
using System;
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
    }
}