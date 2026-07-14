using BE.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.DI.Inward
{
    /// <summary>
    /// Interface repository phiếu nhập kho
    /// </summary>
    public interface IInwardRepo
    {
        /// <summary>
        /// Lấy phiếu nhập theo ID
        /// </summary>
        Task<InwardEntity> GetByIdAsync(Guid inwardId);

        /// <summary>
        /// Lấy tất cả phiếu nhập
        /// </summary>
        Task<IEnumerable<InwardEntity>> GetAllAsync();

        /// <summary>
        /// Thêm phiếu nhập mới
        /// </summary>
        Task<bool> InsertAsync(InwardEntity inward);

        /// <summary>
        /// Cập nhật phiếu nhập
        /// </summary>
        Task<bool> UpdateAsync(InwardEntity inward);

        /// <summary>
        /// Lấy giá nhập gần nhất của sản phẩm
        /// </summary>
        Task<decimal?> GetLatestInwardPriceAsync(Guid productId);

        /// <summary>
        /// Lấy giá bán gần nhất từ phiếu nhập
        /// </summary>
        Task<decimal?> GetLatestSellingPriceAsync(Guid productId);

        /// <summary>
        /// Xóa phiếu nhập theo ID (reverse ledger qua Kafka trước khi xóa)
        /// </summary>
        Task<bool> DeleteAsync(Guid inwardId);
    }
}