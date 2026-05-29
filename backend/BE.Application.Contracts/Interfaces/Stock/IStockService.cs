using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Contracts.Interfaces.Stock
{
    /// <summary>
    /// Interface service kho
    /// </summary>
    public interface IStockService
    {
        /// <summary>
        /// Lấy kho theo ID
        /// </summary>
        Task<StockDto> GetByIdAsync(Guid stockId);

        /// <summary>
        /// Lấy tất cả kho
        /// </summary>
        Task<IEnumerable<StockDto>> GetAllAsync();
    }

    /// <summary>
    /// DTO kho
    /// </summary>
    public class StockDto
    {
        public Guid stock_id { get; set; }
        public string stock_code { get; set; }
        public string stock_name { get; set; }
        public string address { get; set; }
        public DateTime created_date { get; set; }
    }
}