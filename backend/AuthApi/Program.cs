using BE.Application.Contracts.Interfaces;
using BE.Application.Services;
using BE.Domain.DI.User;
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
logger.Info("AuthApi starting...");

// Cấu hình JWT từ config
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

// Đăng ký HttpContextAccessor cho JWT service
builder.Services.AddHttpContextAccessor();

// Đăng ký services
builder.Services.AddScoped<IAuthService, Application.Services.AuthService>();

// Đăng ký repositories (sử dụng connection string từ config)
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Server=localhost;Port=3306;Database=master_db;User=root;Password=Mysql!110720;";
builder.Services.AddScoped<IUserRepo>(sp => new UserRepo(connectionString));

// Add controllers
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// IMPORTANT: Thứ tự middleware - CORS trước Authentication
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "AuthApi" }));

logger.Info("AuthApi started successfully");
app.Run();