using BE.Application.Contracts.Interfaces.Inward;
using BE.Application.Contracts.Interfaces.Outward;
using BE.Application.Contracts.Interfaces.Ledger;
using Microsoft.AspNetCore.Mvc;
using BE.Domain.DI.Outward;
using BE.Domain.DI.Inward;
using BE.Domain.DI.Ledger;

namespace BusinessApi.Controllers
{
    /// <summary>
    /// Controller lấy thông tin giá và tồn kho của sản phẩm
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductPricesController : ControllerBase
    {
        private readonly IOutwardRepo _outwardRepo;
        private readonly IInwardRepo _inwardRepo;
        private readonly ILedgerRepo _ledgerRepo;
        private readonly ILogger<ProductPricesController> _logger;

        public ProductPricesController(
            IOutwardRepo outwardRepo,
            IInwardRepo inwardRepo,
            ILedgerRepo ledgerRepo,
            ILogger<ProductPricesController> logger)
        {
            _outwardRepo = outwardRepo;
            _inwardRepo = inwardRepo;
            _ledgerRepo = ledgerRepo;
            _logger = logger;
        }

        /// <summary>
        /// Lấy giá bán gần nhất của sản phẩm (ưu tiên: selling_price từ inward > giá xuất > giá nhập)
        /// </summary>
        [HttpGet("{productId}/selling-price")]
        public async Task<IActionResult> GetSellingPrice(Guid productId)
        {
            try
            {
                // Ưu tiên 1: Lấy selling_price từ phiếu nhập (giá bán đã được thiết lập)
                var inwardSellingPrice = await _inwardRepo.GetLatestSellingPriceAsync(productId);
                if (inwardSellingPrice.HasValue && inwardSellingPrice.Value > 0)
                {
                    return Ok(new { price = inwardSellingPrice.Value, source = "inward_selling_price" });
                }

                // Ưu tiên 2: Lấy giá xuất (giá bán thực tế) gần nhất
                var outwardPrice = await _outwardRepo.GetLatestOutwardPriceAsync(productId);
                if (outwardPrice.HasValue)
                {
                    return Ok(new { price = outwardPrice.Value, source = "outward" });
                }

                // Ưu tiên 3: Lấy giá nhập gần nhất (fallback)
                var inwardPrice = await _inwardRepo.GetLatestInwardPriceAsync(productId);
                if (inwardPrice.HasValue)
                {
                    return Ok(new { price = inwardPrice.Value, source = "inward" });
                }

                return Ok(new { price = 0, source = "none" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting selling price for product {productId}", productId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin tồn kho của sản phẩm theo kho
        /// </summary>
        [HttpGet("{productId}/stock/{stockId}")]
        public async Task<IActionResult> GetStock(Guid productId, Guid stockId)
        {
            try
            {
                var quantity = await _ledgerRepo.GetClosingQuantityAsync(productId, stockId);

                return Ok(new
                {
                    product_id = productId,
                    stock_id = stockId,
                    quantity = quantity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock for product {productId}, stock {stockId}", productId, stockId);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
