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
}
