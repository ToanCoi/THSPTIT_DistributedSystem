using BE.Application.Contracts.Interfaces.Product;
using BE.Application.Exceptions;
using BE.Domain.DI.Product;
using BE.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Services.Product
{
    /// <summary>
    /// Service xử lý nghiệp vụ sản phẩm
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IProductRepo _productRepo;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepo productRepo, ILogger<ProductService> logger)
        {
            _productRepo = productRepo;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ProductDto> GetByIdAsync(Guid productId)
        {
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null)
            {
                throw new BusinessException("Không tìm thấy sản phẩm", 404);
            }
            return MapToDto(product);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await _productRepo.GetAllAsync();
            var result = new List<ProductDto>();
            foreach (var product in products)
            {
                result.Add(MapToDto(product));
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<ProductDto> CreateAsync(ProductCreateDto dto)
        {
            var product = new ProductEntity
            {
                product_id = Guid.NewGuid(),
                product_code = dto.product_code,
                product_name = dto.product_name,
                price = dto.price,
                unit = dto.unit,
                created_date = DateTime.UtcNow,
                created_by = "system"
            };

            await _productRepo.InsertAsync(product);
            _logger.LogInformation("Tạo sản phẩm mới [{product_id}]", product.product_id);
            return MapToDto(product);
        }

        /// <inheritdoc />
        public async Task<ProductDto> UpdateAsync(Guid productId, ProductUpdateDto dto)
        {
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null)
            {
                throw new BusinessException("Không tìm thấy sản phẩm", 404);
            }

            product.product_code = dto.product_code;
            product.product_name = dto.product_name;
            product.price = dto.price;
            product.unit = dto.unit;
            product.modified_date = DateTime.UtcNow;
            product.modified_by = "system";

            await _productRepo.UpdateAsync(product);
            _logger.LogInformation("Cập nhật sản phẩm [{product_id}]", productId);
            return MapToDto(product);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid productId)
        {
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null)
            {
                throw new BusinessException("Không tìm thấy sản phẩm", 404);
            }

            var result = await _productRepo.DeleteAsync(productId);
            _logger.LogInformation("Xóa sản phẩm [{product_id}]", productId);
            return result;
        }

        private ProductDto MapToDto(ProductEntity product)
        {
            return new ProductDto
            {
                product_id = product.product_id,
                product_code = product.product_code,
                product_name = product.product_name,
                price = product.price,
                unit = product.unit,
                created_date = product.created_date
            };
        }
    }
}