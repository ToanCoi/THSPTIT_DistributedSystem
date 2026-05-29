using BE.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.DI.Product
{
    /// <summary>
    /// Interface repository sản phẩm
    /// </summary>
    public interface IProductRepo
    {
        /// <summary>
        /// Lấy sản phẩm theo ID
        /// </summary>
        Task<ProductEntity> GetByIdAsync(Guid productId);

        /// <summary>
        /// Lấy tất cả sản phẩm
        /// </summary>
        Task<IEnumerable<ProductEntity>> GetAllAsync();

        /// <summary>
        /// Thêm sản phẩm mới
        /// </summary>
        Task<bool> InsertAsync(ProductEntity product);

        /// <summary>
        /// Cập nhật sản phẩm
        /// </summary>
        Task<bool> UpdateAsync(ProductEntity product);

        /// <summary>
        /// Xóa sản phẩm
        /// </summary>
        Task<bool> DeleteAsync(Guid productId);
    }
}