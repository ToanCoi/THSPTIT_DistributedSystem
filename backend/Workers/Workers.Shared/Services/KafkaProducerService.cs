using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Workers.Shared.Services;

/// <summary>
/// Singleton Kafka producer wrapper dùng chung cho mọi service cần publish ledger-change.
/// Có Flush để đảm bảo message đã gửi xong trước khi dispose.
/// </summary>
public interface IKafkaProducerService : IDisposable
{
    Task ProduceAsync(string topic, string key, string value);
}

public class KafkaProducerService : IKafkaProducerService
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(string bootstrapServers, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            SocketTimeoutMs = 5000,
            MessageTimeoutMs = 5000
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, string key, string value)
    {
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = value
        });
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
