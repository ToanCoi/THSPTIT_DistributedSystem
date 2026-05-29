using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Contracts.Interfaces.Inward
{
    /// <summary>
    /// Interface service phiếu nhập kho
    /// </summary>
    public interface IInwardService
    {
        /// <summary>
        /// Lấy phiếu nhập theo ID
        /// </summary>
        Task<InwardDto> GetByIdAsync(Guid inwardId);

        /// <summary>
        /// Lấy tất cả phiếu nhập
        /// </summary>
        Task<IEnumerable<InwardDto>> GetAllAsync();

        /// <summary>
        /// Tạo phiếu nhập mới
        /// </summary>
        Task<InwardDto> CreateAsync(InwardCreateDto dto);
    }

    /// <summary>
    /// DTO phiếu nhập
    /// </summary>
    public class InwardDto
    {
        public Guid inward_id { get; set; }
        public Guid product_id { get; set; }
        public Guid stock_id { get; set; }
        public decimal quantity { get; set; }
        public decimal unit_price { get; set; }
        public string supplier { get; set; }
        public DateTime invoice_date { get; set; }
        public DateTime created_date { get; set; }
    }

    /// <summary>
    /// DTO tạo phiếu nhập
    /// </summary>
    public class InwardCreateDto
    {
        public Guid product_id { get; set; }
        public Guid stock_id { get; set; }
        public decimal quantity { get; set; }
        public decimal unit_price { get; set; }
        public string supplier { get; set; }
        public DateTime invoice_date { get; set; }
    }
}