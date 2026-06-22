using BE.Application.Contracts.Interfaces.Order;
using BE.Application.Services.Order;
using BE.Domain.DI.Order;
using BE.Domain.Mysql;
using BE.Domain.Repos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
    Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message} | ${exception:format=tostring}"
};
var consoleTarget = new ConsoleTarget("logconsole")
{
    Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message} | ${exception:format=tostring}",
    StdErr = true
};
config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);
config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, consoleTarget);
LogManager.Configuration = config;

var logger = NLog.LogManager.GetCurrentClassLogger();
logger.Info("OrderApi starting...");

// Cấu hình JWT
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "Ecom_Microservice_SecretKey_MustBeAtLeast32Characters!2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Ecom.AuthApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Ecom.Client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Lấy connection string cho business_db
var connectionString = builder.Configuration.GetConnectionString("BusinessConnection")
    ?? "Server=localhost;Port=3306;Database=business_db;User=root;Password=Mysql!110720;";

// Đăng ký repositories
builder.Services.AddScoped<IBaseRepo>(sp => new DapperRepo(connectionString));
builder.Services.AddScoped<IOrderRepo>(sp => new OrderRepo(connectionString));
builder.Services.AddScoped<IOrderItemRepo>(sp => new OrderItemRepo(connectionString));

// Đăng ký services
builder.Services.AddScoped<IOrderService, OrderService>();

// Add controllers
builder.Services.AddControllers();

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "OrderApi" }));

logger.Info("OrderApi started successfully");
app.Run();