using BE.Application.Contracts.Interfaces.Outward;
using BE.Domain.Mysql;
using BE.Domain.Repos;
using Confluent.Kafka;
using NLog;
using NLog.Config;
using NLog.Targets;
using Workers.Shared.Models;
using Workers.Shared.Services;

var builder = Host.CreateApplicationBuilder(args);

// NLog configuration
var logConfig = new LoggingConfiguration();
var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDir);

var fileTarget = new FileTarget("logfile")
{
    FileName = Path.Combine(logDir, "${shortdate}.log"),
    Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message} | ${exception:format=tostring}"
};
var consoleTarget = new ConsoleTarget("logconsole")
{
    Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message} | ${exception:format=tostring}",
    StdErr = true
};
logConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);
logConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, consoleTarget);
LogManager.Configuration = logConfig;

var logger = NLog.LogManager.GetCurrentClassLogger();
logger.Info("VoucherWorker starting...");

var connectionString = builder.Configuration.GetConnectionString("BusinessConnection")
    ?? "Server=localhost;Port=3306;Database=business_db;User=root;Password=Mysql!110720;";

// DI for repositories and services
builder.Services.AddScoped<IBaseRepo>(sp => new DapperRepo(connectionString));
builder.Services.AddScoped<BE.Domain.DI.Outward.IOutwardRepo>(sp => new BE.Domain.Mysql.OutwardRepo(connectionString));
builder.Services.AddScoped<IOutwardService, BE.Application.Services.Outward.OutwardService>();

var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9093";
var kafkaTopic = builder.Configuration["Kafka:Topic"] ?? "order-created";
var kafkaGroupId = builder.Configuration["Kafka:GroupId"] ?? "voucher-worker-group";
var ledgerTopic = builder.Configuration["Kafka:LedgerTopic"] ?? "ledger-change";

builder.Services.AddSingleton<IKafkaProducerService>(sp =>
    new KafkaProducerService(kafkaBootstrapServers, sp.GetRequiredService<ILogger<KafkaProducerService>>()));

builder.Services.AddSingleton(new VoucherWorkerSettings
{
    BootstrapServers = kafkaBootstrapServers,
    OrderTopic = kafkaTopic,
    OrderGroupId = kafkaGroupId,
    LedgerTopic = ledgerTopic
});

builder.Services.AddHostedService<VoucherKafkaConsumer>();

var host = builder.Build();
logger.Info("VoucherWorker started successfully");
host.Run();

public class VoucherWorkerSettings
{
    public string BootstrapServers { get; set; } = "localhost:9093";
    public string OrderTopic { get; set; } = "order-created";
    public string OrderGroupId { get; set; } = "voucher-worker-group";
    public string LedgerTopic { get; set; } = "ledger-change";
}

public class VoucherKafkaConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly VoucherWorkerSettings _settings;
    private readonly ILogger<VoucherKafkaConsumer> _logger;
    private IConsumer<string, string> _consumer;
    private IProducer<string, string> _producer;

    public VoucherKafkaConsumer(
        IServiceProvider serviceProvider,
        VoucherWorkerSettings settings,
        ILogger<VoucherKafkaConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.OrderGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            Acks = Acks.All
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();

        _consumer.Subscribe(_settings.OrderTopic);
        _logger.LogInformation("VoucherWorker started, consuming topic [{topic}]", _settings.OrderTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result?.Message != null)
                {
                    await ProcessMessageAsync(result.Message.Value);
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
        _producer.Flush();
    }

    private async Task ProcessMessageAsync(string message)
    {
        _logger.LogInformation("Received order-created message: {message}", message);

        try
        {
            var orderMsg = System.Text.Json.JsonSerializer.Deserialize<OrderCreatedMessage>(message);
            if (orderMsg == null)
            {
                _logger.LogWarning("Failed to parse order message: null");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var outwardService = scope.ServiceProvider.GetRequiredService<IOutwardService>();

            foreach (var item in orderMsg.items)
            {
                var outwardDto = new BE.Application.Contracts.Interfaces.Outward.OutwardCreateDto
                {
                    order_id = Guid.Parse(orderMsg.order_id),
                    product_id = Guid.Parse(item.product_id),
                    stock_id = Guid.Parse(orderMsg.stock_id),
                    quantity = item.quantity,
                    unit_price = item.unit_price,
                    outward_date = DateTime.UtcNow
                };

                var outward = await outwardService.CreateAsync(outwardDto);
                _logger.LogInformation("Created outward [{outward_id}] for order {order_id}",
                    outward.outward_id, orderMsg.order_id);

                // Push ledger-change
                var ledgerMsg = new LedgerChangeMessage
                {
                    voucher_id = outward.outward_id.ToString(),
                    voucher_type = "OUTWARD",
                    product_id = item.product_id,
                    stock_id = orderMsg.stock_id,
                    quantity = item.quantity,
                    timestamp = DateTime.UtcNow.ToString("o")
                };

                var json = System.Text.Json.JsonSerializer.Serialize(ledgerMsg);
                await _producer.ProduceAsync(_settings.LedgerTopic, new Message<string, string>
                {
                    Key = outward.outward_id.ToString(),
                    Value = json
                });

                _logger.LogInformation("Published ledger-change for outward [{outward_id}]", outward.outward_id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order message: {message}", message);
        }
    }
}
