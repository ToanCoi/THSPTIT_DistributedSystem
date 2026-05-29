using BE.Application.Contracts.Interfaces;
using BE.Application.Contracts.Interfaces.Customer;
using BE.Application.Contracts.Interfaces.Product;
using BE.Application.Contracts.Interfaces.Stock;
using BE.Application.Contracts.Interfaces.Inward;
using BE.Application.Contracts.Interfaces.Outward;
using BE.Application.Services;
using BE.Application.Services.Customer;
using BE.Application.Services.Product;
using BE.Application.Services.Stock;
using BE.Application.Services.Inward;
using BE.Application.Services.Outward;
using BE.Domain.DI.Customer;
using BE.Domain.DI.Product;
using BE.Domain.DI.Stock;
using BE.Domain.DI.Inward;
using BE.Domain.DI.Outward;
using BE.Domain.Mysql;
using BE.HostBase.Extensions;
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
config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);
LogManager.Configuration = config;

var logger = NLog.LogManager.GetCurrentClassLogger();
logger.Info("BusinessApi starting...");

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
builder.Services.AddScoped<ICustomerRepo>(sp => new CustomerRepo(connectionString));
builder.Services.AddScoped<IProductRepo>(sp => new ProductRepo(connectionString));
builder.Services.AddScoped<IStockRepo>(sp => new StockRepo(connectionString));
builder.Services.AddScoped<IInwardRepo>(sp => new InwardRepo(connectionString));
builder.Services.AddScoped<IOutwardRepo>(sp => new OutwardRepo(connectionString));

// Đăng ký services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IInwardService, InwardService>();
builder.Services.AddScoped<IOutwardService, OutwardService>();

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

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "BusinessApi" }));

logger.Info("BusinessApi started successfully");
app.Run();