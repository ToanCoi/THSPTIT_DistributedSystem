using BE.Application.Contracts.Interfaces.Ledger;
using BE.Domain.DI.Ledger;
using BE.Domain.Mysql;
using HandleWorker;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Text;
using Confluent.Kafka;

var builder = Host.CreateApplicationBuilder(args);

// Cấu hình NLog
var config = new LoggingConfiguration();
var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDirectory);

var fileTarget = new FileTarget("logfile")
{
    FileName = Path.Combine(logDirectory, "${shortdate}.log"),
    Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message} | ${exception:format=tostring}"
};
config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);
LogManager.Configuration = config;

var logger = NLog.LogManager.GetCurrentClassLogger();
logger.Info("HandleWorker starting...");

// Lấy connection string cho business_db
var connectionString = builder.Configuration.GetConnectionString("BusinessConnection")
    ?? "Server=localhost;Port=3306;Database=business_db;User=root;Password=Mysql!110720;";

// Đăng ký Ledger repository
builder.Services.AddScoped<ILedgerRepo>(sp => new LedgerRepo(connectionString));

// Đăng ký Ledger service
builder.Services.AddScoped<ILedgerService, LedgerService>();

// Đăng ký Kafka consumer
builder.Services.AddHostedService<KafkaConsumerService>();

var host = builder.Build();

logger.Info("HandleWorker started successfully");
host.Run();

/// <summary>
/// Service xử lý Kafka consumer cho topic order-created
/// </summary>
public class KafkaConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerService> _logger;
    private IConsumer<string, string> _consumer;
    private readonly string _topic = "order-created";
    private readonly string _groupId = "handle-worker-group";

    public KafkaConsumerService(IServiceProvider serviceProvider, ILogger<KafkaConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _consumer.Subscribe(_topic);

        _logger.LogInformation("Kafka consumer started, listening to topic [{topic}]", _topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                if (consumeResult?.Message != null)
                {
                    await ProcessMessageAsync(consumeResult.Message.Value);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming Kafka message");
            }
        }

        _consumer.Close();
    }

    private async Task ProcessMessageAsync(string message)
    {
        _logger.LogInformation("Received message: {message}", message);

        try
        {
            // Parse JSON message
            var orderMessage = System.Text.Json.JsonSerializer.Deserialize<OrderCreatedMessage>(message);
            if (orderMessage == null)
            {
                _logger.LogWarning("Failed to parse message: null");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var ledgerService = scope.ServiceProvider.GetRequiredService<ILedgerService>();

            // Xử lý từng item trong đơn hàng
            foreach (var item in orderMessage.items)
            {
                await ledgerService.ProcessOrderItemAsync(
                    Guid.Parse(orderMessage.order_id),
                    Guid.Parse(item.product_id),
                    item.quantity,
                    item.unit_price
                );
            }

            _logger.LogInformation("Processed order [{order_id}] with {item_count} items",
                orderMessage.order_id, orderMessage.items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {message}", message);
        }
    }
}

/// <summary>
/// Class parsed từ Kafka message
/// </summary>
public class OrderCreatedMessage
{
    public string order_id { get; set; }
    public string customer_id { get; set; }
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