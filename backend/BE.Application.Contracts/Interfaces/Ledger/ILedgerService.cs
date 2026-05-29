using System;
using System.Threading.Tasks;

namespace BE.Application.Contracts.Interfaces.Ledger
{
    /// <summary>
    /// Interface service sổ cái tồn kho
    /// </summary>
    public interface ILedgerService
    {
        /// <summary>
        /// Xử lý một item trong đơn hàng - ghi vào các bảng ledger
        /// </summary>
        Task ProcessOrderItemAsync(Guid orderId, Guid productId, decimal quantity, decimal unitPrice);

        /// <summary>
        /// Xử lý phiếu nhập kho
        /// </summary>
        Task ProcessInwardAsync(Guid inwardId, Guid productId, Guid stockId, decimal quantity, decimal unitPrice);

        /// <summary>
        /// Xử lý phiếu xuất kho
        /// </summary>
        Task ProcessOutwardAsync(Guid outwardId, Guid productId, Guid stockId, decimal quantity, decimal unitPrice);
    }
}