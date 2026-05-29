using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Contracts.Interfaces.Product
{
    /// <summary>
    /// Interface service sản phẩm
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Lấy sản phẩm theo ID
        /// </summary>
        Task<ProductDto> GetByIdAsync(Guid productId);

        /// <summary>
        /// Lấy tất cả sản phẩm
        /// </summary>
        Task<IEnumerable<ProductDto>> GetAllAsync();

        /// <summary>
        /// Tạo sản phẩm mới
        /// </summary>
        Task<ProductDto> CreateAsync(ProductCreateDto dto);

        /// <summary>
        /// Cập nhật sản phẩm
        /// </summary>
        Task<ProductDto> UpdateAsync(Guid productId, ProductUpdateDto dto);

        /// <summary>
        /// Xóa sản phẩm
        /// </summary>
        Task<bool> DeleteAsync(Guid productId);
    }

    /// <summary>
    /// DTO sản phẩm
    /// </summary>
    public class ProductDto
    {
        public Guid product_id { get; set; }
        public string product_code { get; set; }
        public string product_name { get; set; }
        public decimal price { get; set; }
        public string unit { get; set; }
        public DateTime created_date { get; set; }
    }

    /// <summary>
    /// DTO tạo sản phẩm
    /// </summary>
    public class ProductCreateDto
    {
        public string product_code { get; set; }
        public string product_name { get; set; }
        public decimal price { get; set; }
        public string unit { get; set; }
    }

    /// <summary>
    /// DTO cập nhật sản phẩm
    /// </summary>
    public class ProductUpdateDto
    {
        public string product_code { get; set; }
        public string product_name { get; set; }
        public decimal price { get; set; }
        public string unit { get; set; }
    }
}