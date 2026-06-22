namespace Workers.Shared.Services;

public class KafkaProducerSettings
{
    public string BootstrapServers { get; set; } = "localhost:9093";
    public string LedgerTopic { get; set; } = "ledger-change";
}
