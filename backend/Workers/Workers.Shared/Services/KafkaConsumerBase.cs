using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Workers.Shared.Services;

/// <summary>
/// Base class cho Kafka consumer worker service
/// </summary>
public abstract class KafkaConsumerBase : BackgroundService
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ILogger Logger;
    protected readonly string BootstrapServers;
    protected readonly string Topic;
    protected readonly string GroupId;
    private IConsumer<string, string> _consumer;

    protected KafkaConsumerBase(
        IServiceProvider serviceProvider,
        string bootstrapServers,
        string topic,
        string groupId,
        ILogger logger)
    {
        ServiceProvider = serviceProvider;
        BootstrapServers = bootstrapServers;
        Topic = topic;
        GroupId = groupId;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(Topic);

        Logger.LogInformation("Kafka consumer started, listening to topic [{topic}]", Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result?.Message != null)
                {
                    await HandleMessageAsync(result.Message.Value, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error consuming Kafka message");
            }
        }

        _consumer.Close();
    }

    protected abstract Task HandleMessageAsync(string message, CancellationToken cancellationToken);
}
