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
        /// Xử lý phiếu nhập kho
        /// </summary>
        Task ProcessInwardAsync(Guid inwardId, Guid productId, Guid stockId, decimal quantity);

        /// <summary>
        /// Xử lý phiếu xuất kho
        /// </summary>
        Task ProcessOutwardAsync(Guid outwardId, Guid productId, Guid stockId, decimal quantity);

        /// <summary>
        /// Xử lý item trong đơn hàng - dùng cho worker consume order-created.
        /// Tự sinh ledger entry dựa trên quantity dương/âm.
        /// </summary>
        Task ProcessOrderItemAsync(Guid orderId, Guid productId, Guid stockId, decimal quantity, decimal unitPrice);

        /// <summary>
        /// Xử lý cập nhật phiếu nhập/xuất: tính delta và ghi ledger entry điều chỉnh.
        /// Xử lý được cả trường hợp đổi stock (A → B): reverse entry cho A, insert entry cho B.
        /// </summary>
        Task ProcessUpdateAsync(
            Guid voucherId,
            string voucherType,
            Guid newProductId,
            Guid newStockId,
            decimal newQuantity,
            decimal oldQuantity,
            Guid oldProductId,
            Guid oldStockId);
    }
}