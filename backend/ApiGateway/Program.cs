using NLog;
using NLog.Config;
using NLog.Targets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình NLog
var config = new LoggingConfiguration();
var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDirectory);

var fileTarget = new FileTarget("logfile")
{
    FileName = Path.Combine(logDirectory, "${shortdate}.log"),
    Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message}"
};
config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);
LogManager.Configuration = config;

var logger = NLog.LogManager.GetCurrentClassLogger();
logger.Info("ApiGateway starting...");

// Đăng ký HttpClient cho các service
builder.Services.AddHttpClient("Auth", c =>
{
    c.BaseAddress = new Uri("http://localhost:5289");
    c.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("Business", c =>
{
    c.BaseAddress = new Uri("http://localhost:5119");
    c.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("Order", c =>
{
    c.BaseAddress = new Uri("http://localhost:5120");
    c.Timeout = TimeSpan.FromSeconds(30);
});

// Đăng ký ProxyService
builder.Services.AddScoped<ProxyService>();

// Add controllers (cho health check)
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.MapControllers();

// Route: /Auth/* -> AuthApi:5001
app.Map("/Auth/{**path}", async context =>
{
    var proxy = context.RequestServices.GetRequiredService<ProxyService>();
    await proxy.ProxyAsync("Auth", $"/Auth/{context.Request.RouteValues["path"]}", context);
});

// Route: /Business/* -> BusinessApi:5002
app.Map("/Business/{**path}", async context =>
{
    var proxy = context.RequestServices.GetRequiredService<ProxyService>();
    await proxy.ProxyAsync("Business", $"/api/{context.Request.RouteValues["path"]}", context);
});

// Route: /Order/* -> OrderApi:5003
app.Map("/Order/{**path}", async context =>
{
    var proxy = context.RequestServices.GetRequiredService<ProxyService>();
    await proxy.ProxyAsync("Order", $"/api/{context.Request.RouteValues["path"]}", context);
});

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ApiGateway" }));

logger.Info("ApiGateway started successfully");
app.Run();

/// <summary>
/// Service xử lý proxy request sang các backend service
/// </summary>
public class ProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProxyService> _logger;

    public ProxyService(IHttpClientFactory httpClientFactory, ILogger<ProxyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Forward request sang backend service
    /// </summary>
    public async Task ProxyAsync(string serviceName, string path, HttpContext context)
    {
        var client = _httpClientFactory.CreateClient(serviceName);

        try
        {
            // Build request URL
            var requestPath = $"{path}{context.Request.QueryString}";

            // Tạo request mới
            var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), requestPath);

            // Copy headers
            foreach (var header in context.Request.Headers)
            {
                if (header.Key != "Host")
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            // Copy body
            if (context.Request.ContentLength > 0 || context.Request.ContentType != null)
            {
                request.Content = new StreamContent(context.Request.Body);
                if (!string.IsNullOrEmpty(context.Request.ContentType))
                {
                    request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType);
                }
            }

            // Gửi request
            var response = await client.SendAsync(request);

            // Copy response
            context.Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers)
            {
                if (header.Key != "Transfer-Encoding") // Avoid chunked encoding issues
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }
            foreach (var header in response.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            // Copy content
            var content = await response.Content.ReadAsByteArrayAsync();
            await context.Response.Body.WriteAsync(content);

            _logger.LogInformation("Proxied {method} {path} -> {service} -> {status}",
                context.Request.Method, path, serviceName, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {service}: {path}", serviceName, path);
            context.Response.StatusCode = 502;
            await context.Response.WriteAsync($"Gateway Error: {ex.Message}");
        }
    }
}