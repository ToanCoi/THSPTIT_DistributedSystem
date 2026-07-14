using BE.Application.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Contracts.Interfaces.Outward
{
    /// <summary>
    /// Interface service phiếu xuất kho
    /// </summary>
    public interface IOutwardService
    {
        /// <summary>
        /// Lấy phiếu xuất theo ID
        /// </summary>
        Task<OutwardDto> GetByIdAsync(Guid outwardId);

        /// <summary>
        /// Lấy tất cả phiếu xuất
        /// </summary>
        Task<IEnumerable<OutwardDto>> GetAllAsync();

        /// <summary>
        /// Lấy danh sách phân trang
        /// </summary>
        Task<PagingResult<OutwardDto>> GetAllPagingAsync(PagingFilterDto filter);

        /// <summary>
        /// Tạo phiếu xuất mới
        /// </summary>
        Task<OutwardDto> CreateAsync(OutwardCreateDto dto);

        /// <summary>
        /// Cập nhật phiếu xuất. Lưu ý: phiếu xuất có gắn với đơn hàng (order_id) không thể sửa.
        /// </summary>
        Task<OutwardDto> UpdateAsync(Guid outwardId, OutwardUpdateDto dto);

        /// <summary>
        /// Xóa phiếu xuất. Phiếu xuất gắn với đơn hàng (order_id) không thể xóa trực tiếp — phải xóa đơn trước.
        /// </summary>
        Task<bool> RemoveAsync(Guid outwardId);
    }

    /// <summary>
    /// DTO phiếu xuất
    /// </summary>
    public class OutwardDto
    {
        public Guid outward_id { get; set; }
        public Guid? order_id { get; set; }
        public string? order_code { get; set; }
        public Guid product_id { get; set; }
        public string? product_name { get; set; }
        public Guid stock_id { get; set; }
        public string? stock_name { get; set; }
        public decimal quantity { get; set; }
        public decimal unit_price { get; set; }
        public DateTime outward_date { get; set; }
        public DateTime created_date { get; set; }
    }

    /// <summary>
    /// DTO tạo phiếu xuất
    /// </summary>
    public class OutwardCreateDto
    {
        public Guid? order_id { get; set; }
        public Guid product_id { get; set; }
        public Guid stock_id { get; set; }
        public decimal quantity { get; set; }
        public decimal unit_price { get; set; }
        public DateTime outward_date { get; set; }
    }

    /// <summary>
    /// DTO cập nhật phiếu xuất (không có order_id vì form thủ công không cho chọn đơn hàng)
    /// </summary>
    public class OutwardUpdateDto
    {
        public Guid product_id { get; set; }
        public Guid stock_id { get; set; }
        public decimal quantity { get; set; }
        public decimal unit_price { get; set; }
        public DateTime outward_date { get; set; }
    }
}