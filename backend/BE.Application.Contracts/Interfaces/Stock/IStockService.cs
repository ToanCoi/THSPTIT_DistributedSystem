using BE.Application.Contracts.Dtos;
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

        /// <summary>
        /// Lấy danh sách phân trang
        /// </summary>
        Task<PagingResult<StockDto>> GetAllPagingAsync(PagingFilterDto filter);

        /// <summary>
        /// Tạo kho mới
        /// </summary>
        Task<StockDto> CreateAsync(StockCreateDto dto);

        /// <summary>
        /// Cập nhật kho
        /// </summary>
        Task<StockDto> UpdateAsync(Guid stockId, StockCreateDto dto);

        /// <summary>
        /// Xóa kho
        /// </summary>
        Task<bool> DeleteAsync(Guid stockId);
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

    /// <summary>
    /// DTO tạo kho
    /// </summary>
    public class StockCreateDto
    {
        public string stock_code { get; set; }
        public string stock_name { get; set; }
        public string address { get; set; }
    }
}
