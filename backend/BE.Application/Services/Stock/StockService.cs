using BE.Application.Contracts.Dtos;
using BE.Application.Contracts.Interfaces.Stock;
using BE.Application.Exceptions;
using BE.Domain.DI.Stock;
using BE.Domain.Entities;
using BE.Domain.Repos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BE.Application.Services.Stock
{
    /// <summary>
    /// Service xử lý nghiệp vụ kho
    /// </summary>
    public class StockService : IStockService
    {
        private readonly IStockRepo _stockRepo;
        private readonly IBaseRepo _baseRepo;
        private readonly ILogger<StockService> _logger;

        public StockService(IStockRepo stockRepo, IBaseRepo baseRepo, ILogger<StockService> logger)
        {
            _stockRepo = stockRepo;
            _baseRepo = baseRepo;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<StockDto> GetByIdAsync(Guid stockId)
        {
            var stock = await _stockRepo.GetByIdAsync(stockId);
            if (stock == null)
            {
                throw new BusinessException("Không tìm thấy kho", 404);
            }
            return MapToDto(stock);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StockDto>> GetAllAsync()
        {
            var stocks = await _stockRepo.GetAllAsync();
            var result = new List<StockDto>();
            foreach (var stock in stocks)
            {
                result.Add(MapToDto(stock));
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<PagingResult<StockDto>> GetAllPagingAsync(PagingFilterDto filter)
        {
            var columns = "stock_id, stock_code, stock_name, address, created_date";
            var sort = $"{filter.sort_field} {filter.sort_order}";

            var pagingResult = await _baseRepo.GetPaging<StockEntity>(
                columns,
                filter.skip,
                filter.take,
                sort,
                null
            );

            var dtos = new List<StockDto>();
            if (pagingResult.Data != null)
            {
                foreach (var entity in pagingResult.Data.Cast<StockEntity>())
                {
                    dtos.Add(MapToDto(entity));
                }
            }

            return new PagingResult<StockDto>
            {
                data = dtos,
                total = dtos.Count
            };
        }

        /// <inheritdoc />
        public async Task<StockDto> CreateAsync(StockCreateDto dto)
        {
            var stock = new StockEntity
            {
                stock_id = Guid.NewGuid(),
                stock_code = dto.stock_code,
                stock_name = dto.stock_name,
                address = dto.address,
                created_date = DateTime.Now
            };

            await _baseRepo.InsertAsync<StockEntity>(stock);
            return MapToDto(stock);
        }

        /// <inheritdoc />
        public async Task<StockDto> UpdateAsync(Guid stockId, StockCreateDto dto)
        {
            var stock = await _stockRepo.GetByIdAsync(stockId);
            if (stock == null)
            {
                throw new BusinessException("Không tìm thấy kho", 404);
            }

            stock.stock_code = dto.stock_code;
            stock.stock_name = dto.stock_name;
            stock.address = dto.address;
            stock.modified_date = DateTime.Now;

            await _baseRepo.UpdateAsync<StockEntity>(stock);
            return MapToDto(stock);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid stockId)
        {
            var stock = await _stockRepo.GetByIdAsync(stockId);
            if (stock == null)
            {
                throw new BusinessException("Không tìm thấy kho", 404);
            }

            return await _baseRepo.DeleteAsync(stock);
        }

        private StockDto MapToDto(StockEntity stock)
        {
            return new StockDto
            {
                stock_id = stock.stock_id,
                stock_code = stock.stock_code,
                stock_name = stock.stock_name,
                address = stock.address,
                created_date = stock.created_date
            };
        }
    }
}
