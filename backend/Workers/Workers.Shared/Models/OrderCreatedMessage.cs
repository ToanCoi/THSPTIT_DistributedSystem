namespace Workers.Shared.Models;

/// <summary>
/// Message parsed từ Kafka topic order-created
/// </summary>
public class OrderCreatedMessage
{
    public string order_id { get; set; }
    public string customer_id { get; set; }
    public string stock_id { get; set; }
    public string order_code { get; set; }
    public List<OrderItemMessage> items { get; set; } = new();
    public string timestamp { get; set; }
}

public class OrderItemMessage
{
    public string product_id { get; set; }
    public decimal quantity { get; set; }
    public decimal unit_price { get; set; }
}
