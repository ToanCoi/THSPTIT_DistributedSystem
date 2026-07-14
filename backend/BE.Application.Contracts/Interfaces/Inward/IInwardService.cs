using BE.Application.Contracts.Dtos;
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
        /// Lấy danh sách phân trang
        /// </summary>
        Task<PagingResult<InwardDto>> GetAllPagingAsync(PagingFilterDto filter);

        /// <summary>
        /// Tạo phiếu nhập mới
        /// </summary>
        Task<InwardDto> CreateAsync(InwardCreateDto dto);

        /// <summary>
        /// Cập nhật phiếu nhập
        /// </summary>
        Task<InwardDto> UpdateAsync(Guid inwardId, InwardUpdateDto dto);

        /// <summary>
        /// Xóa phiếu nhập (publish ledger UPDATE quantity=0 để reverse, rồi xóa DB)
        /// </summary>
        Task<bool> RemoveAsync(Guid inwardId);
    }

    /// <summary>
    /// DTO phiếu nhập
    /// </summary>
    public class InwardDto
    {
        public Guid inward_id { get; set; }
        public Guid product_id { get; set; }
        public string? product_name { get; set; }
        public Guid stock_id { get; set; }
        public string? stock_name { get; set; }
        public decimal quantity { get; set; }
        public decimal unit_price { get; set; }
        public decimal selling_price { get; set; }
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
        public decimal selling_price { get; set; }
        public string supplier { get; set; }
        public DateTime invoice_date { get; set; }
    }

    /// <summary>
    /// DTO cập nhật phiếu nhập
    /// </summary>
    public class InwardUpdateDto
    {
        public Guid product_id { get; set; }
        public Guid stock_id { get; set; }
        public decimal quantity { get; set; }
        public decimal unit_price { get; set; }
        public decimal selling_price { get; set; }
        public string supplier { get; set; }
        public DateTime invoice_date { get; set; }
    }
}