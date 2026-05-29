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
    }
}