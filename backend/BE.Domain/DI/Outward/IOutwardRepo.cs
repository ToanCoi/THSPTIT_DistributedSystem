using BE.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.DI.Outward
{
    /// <summary>
    /// Interface repository phiếu xuất kho
    /// </summary>
    public interface IOutwardRepo
    {
        /// <summary>
        /// Lấy phiếu xuất theo ID
        /// </summary>
        Task<OutwardEntity> GetByIdAsync(Guid outwardId);

        /// <summary>
        /// Lấy tất cả phiếu xuất
        /// </summary>
        Task<IEnumerable<OutwardEntity>> GetAllAsync();

        /// <summary>
        /// Thêm phiếu xuất mới
        /// </summary>
        Task<bool> InsertAsync(OutwardEntity outward);

        /// <summary>
        /// Cập nhật phiếu xuất
        /// </summary>
        Task<bool> UpdateAsync(OutwardEntity outward);

        /// <summary>
        /// Lấy giá xuất gần nhất của sản phẩm (giá bán)
        /// </summary>
        Task<decimal?> GetLatestOutwardPriceAsync(Guid productId);

        /// <summary>
        /// Lấy các phiếu xuất gắn với đơn hàng (dùng cho cascade delete Order)
        /// </summary>
        Task<IEnumerable<OutwardEntity>> GetByOrderIdAsync(Guid orderId);

        /// <summary>
        /// Xóa phiếu xuất theo ID
        /// </summary>
        Task<bool> DeleteAsync(Guid outwardId);

        /// <summary>
        /// Xóa tất cả phiếu xuất gắn với đơn hàng (dùng cho cascade delete Order)
        /// </summary>
        Task<bool> DeleteByOrderIdAsync(Guid orderId);
    }
}