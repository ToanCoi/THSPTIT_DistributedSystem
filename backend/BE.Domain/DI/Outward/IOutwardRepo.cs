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
    }
}