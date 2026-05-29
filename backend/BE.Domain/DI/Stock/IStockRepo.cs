using BE.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Domain.DI.Stock
{
    /// <summary>
    /// Interface repository kho
    /// </summary>
    public interface IStockRepo
    {
        /// <summary>
        /// Lấy kho theo ID
        /// </summary>
        Task<StockEntity> GetByIdAsync(Guid stockId);

        /// <summary>
        /// Lấy tất cả kho
        /// </summary>
        Task<IEnumerable<StockEntity>> GetAllAsync();
    }
}