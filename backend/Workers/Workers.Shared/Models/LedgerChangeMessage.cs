namespace Workers.Shared.Models;

/// <summary>
/// Message gửi qua Kafka topic ledger-change
/// </summary>
public class LedgerChangeMessage
{
    public string voucher_id { get; set; }
    public string voucher_type { get; set; }
    public string product_id { get; set; }
    public string stock_id { get; set; }
    public decimal quantity { get; set; }
    public string timestamp { get; set; }

    /// <summary>
    /// Loại sự kiện: CREATE (mặc định) hoặc UPDATE
    /// Khi UPDATE, cần kèm old_quantity, old_product_id, old_stock_id để tính delta
    /// </summary>
    public string event_type { get; set; } = "CREATE";

    /// <summary>
    /// Số lượng cũ - chỉ dùng khi event_type = UPDATE
    /// </summary>
    public decimal? old_quantity { get; set; }

    /// <summary>
    /// ID sản phẩm cũ - chỉ dùng khi event_type = UPDATE
    /// </summary>
    public string? old_product_id { get; set; }

    /// <summary>
    /// ID kho cũ - chỉ dùng khi event_type = UPDATE
    /// </summary>
    public string? old_stock_id { get; set; }
}