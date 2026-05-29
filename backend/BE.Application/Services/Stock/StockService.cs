using BE.Application.Contracts.Interfaces.Stock;
using BE.Application.Exceptions;
using BE.Domain.DI.Stock;
using BE.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BE.Application.Services.Stock
{
    /// <summary>
    /// Service xử lý nghiệp vụ kho
    /// </summary>
    public class StockService : IStockService
    {
        private readonly IStockRepo _stockRepo;
        private readonly ILogger<StockService> _logger;

        public StockService(IStockRepo stockRepo, ILogger<StockService> logger)
        {
            _stockRepo = stockRepo;
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