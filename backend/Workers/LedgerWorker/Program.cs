using BE.Application.Contracts.Interfaces.Ledger;
using BE.Domain.DI.Ledger;
using BE.Domain.Mysql;
using NLog;
using NLog.Config;
using NLog.Targets;
using Workers.LedgerWorker;
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
logger.Info("LedgerWorker starting...");

var connectionString = builder.Configuration.GetConnectionString("BusinessConnection")
    ?? "Server=localhost;Port=3306;Database=business_db;User=root;Password=Mysql!110720;";

builder.Services.AddScoped<ILedgerRepo>(sp => new LedgerRepo(connectionString));
builder.Services.AddScoped<ILedgerService, LedgerService>();

builder.Services.AddSingleton(sp => new KafkaProducerSettings
{
    BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9093",
    LedgerTopic = builder.Configuration["Kafka:LedgerTopic"] ?? "ledger-change"
});

builder.Services.AddSingleton(sp => new KafkaConsumerSettings
{
    BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9093",
    Topic = builder.Configuration["Kafka:LedgerTopic"] ?? "ledger-change",
    GroupId = builder.Configuration["Kafka:LedgerGroupId"] ?? "ledger-worker-group"
});
builder.Services.AddHostedService<LedgerKafkaConsumer>();

var host = builder.Build();
logger.Info("LedgerWorker started successfully");
host.Run();

public class KafkaConsumerSettings
{
    public string BootstrapServers { get; set; } = "localhost:9093";
    public string Topic { get; set; } = "ledger-change";
    public string GroupId { get; set; } = "ledger-worker-group";
}

public class LedgerKafkaConsumer : KafkaConsumerBase
{
    public LedgerKafkaConsumer(
        IServiceProvider serviceProvider,
        KafkaConsumerSettings settings,
        ILogger<LedgerKafkaConsumer> logger)
        : base(serviceProvider, settings.BootstrapServers, settings.Topic, settings.GroupId, logger)
    {
    }

    protected override async Task HandleMessageAsync(string message, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Received ledger-change message: {message}", message);

        try
        {
            var ledgerMsg = System.Text.Json.JsonSerializer.Deserialize<LedgerChangeMessage>(message);
            if (ledgerMsg == null)
            {
                Logger.LogWarning("Failed to parse ledger-change message: null");
                return;
            }

            using var scope = ServiceProvider.CreateScope();
            var ledgerService = scope.ServiceProvider.GetRequiredService<ILedgerService>();

            if (ledgerMsg.voucher_type == "INWARD")
            {
                await ledgerService.ProcessInwardAsync(
                    Guid.Parse(ledgerMsg.voucher_id),
                    Guid.Parse(ledgerMsg.product_id),
                    Guid.Parse(ledgerMsg.stock_id),
                    ledgerMsg.quantity
                );
            }
            else if (ledgerMsg.voucher_type == "OUTWARD")
            {
                await ledgerService.ProcessOutwardAsync(
                    Guid.Parse(ledgerMsg.voucher_id),
                    Guid.Parse(ledgerMsg.product_id),
                    Guid.Parse(ledgerMsg.stock_id),
                    ledgerMsg.quantity
                );
            }
            else
            {
                Logger.LogWarning("Unknown voucher_type: {voucher_type}", ledgerMsg.voucher_type);
            }

            Logger.LogInformation("Processed ledger-change for voucher [{voucher_id}] type={voucher_type}",
                ledgerMsg.voucher_id, ledgerMsg.voucher_type);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing ledger-change message: {message}", message);
        }
    }
}
