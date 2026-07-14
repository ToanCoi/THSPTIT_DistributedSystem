# Cấu trúc code backend

Tài liệu này mô tả cách tổ chức code backend theo **Clean Architecture**, các pattern đang dùng, và quy ước code.

## 1. Tổng quan solution

```
backend/
├── Ecom.slnx                    # Solution file
├── ApiGateway/                  # 1 .NET Web project (port 5000)
├── AuthApi/                     # 1 .NET Web API project (port 5001)
├── BusinessApi/                 # 1 .NET Web API project (port 5002)
├── OrderApi/                    # 1 .NET Web API project (port 5003)
├── Workers/
│   ├── LedgerWorker/            # 1 .NET Worker Service (background)
│   ├── VoucherWorker/           # 1 .NET Worker Service (background)
│   └── Workers.Shared/          # Shared library (Kafka helpers)
├── BE.Domain/                   # Class library — entities + repo interfaces
├── BE.Domain.Mysql/             # Class library — repo implementations
├── BE.Domain.Share/             # Class library — share models
├── BE.Application/              # Class library — service implementations
├── BE.Application.Contracts/    # Class library — service interfaces + DTOs
├── BE.HostBase/                 # Class library — DI extensions
├── Scripts/                     # SQL init + migrations
└── docker-compose.yml           # Dev local
```

## 2. Clean Architecture layers

Hệ thống chia code thành **4 layer** + 1 shared lib:

```
┌─────────────────────────────────────────────────────────────┐
│  HOST LAYER (ApiGateway, AuthApi, BusinessApi, OrderApi,    │
│              Workers)                                        │
│  - Program.cs (DI registration, middleware, routing)        │
│  - Controllers                                                │
│  - appsettings.json                                          │
└─────────────────────────────────────────────────────────────┘
                            │ tham chiếu
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  APPLICATION LAYER (BE.Application.Contracts)                 │
│  - Service interfaces (IOrderService, ICustomerService, ...)  │
│  - DTOs (OrderCreateDto, CustomerDto, ...)                   │
│  - Exceptions (BusinessException)                            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  APPLICATION IMPLEMENTATION (BE.Application)                 │
│  - Service implementations (OrderService, CustomerService)    │
│  - BusinessException                                         │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  DOMAIN LAYER (BE.Domain)                                    │
│  - Entities (OrderEntity, CustomerEntity, ...)               │
│  - Repository interfaces (IOrderRepo, ICustomerRepo, ...)    │
│  - Dapper query helpers                                       │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  INFRASTRUCTURE (BE.Domain.Mysql)                            │
│  - Repository implementations (OrderRepo, CustomerRepo, ...) │
│  - DapperRepo (BaseRepo)                                     │
└─────────────────────────────────────────────────────────────┘
```

### Quy tắc dependency

- **Host** → tham chiếu **Application.Contracts** + **Application** + **Domain** + **Domain.Mysql** + **Workers.Shared** (tùy service)
- **Application** → tham chiếu **Domain**
- **Application.Contracts** → (không tham chiếu ai — chỉ chứa interface + DTO)
- **Domain** → (không tham chiếu ai — pure domain)
- **Domain.Mysql** → tham chiếu **Domain** + **MySqlConnector**
- **Workers.Shared** → (chỉ tham chiếu Confluent.Kafka)

### Lưu ý

- Host project tham chiếu cả Application lẫn Application.Contracts. Trong thiết kế Clean Architecture "thuần", Host chỉ nên tham chiếu Application.Contracts, còn Application impl nên được load qua reflection hoặc DI tự động. Trong project này, host tham chiếu trực tiếp Application cho đơn giản.

## 3. Chi tiết từng project

### 3.1 `BE.Domain/`

Chứa **pure domain** — không phụ thuộc infrastructure.

```
BE.Domain/
├── Entities/
│   ├── CustomerEntity.cs
│   ├── ProductEntity.cs
│   ├── StockEntity.cs
│   ├── InwardEntity.cs
│   ├── OutwardEntity.cs
│   ├── OrderEntity.cs
│   ├── OrderItemEntity.cs
│   ├── LedgerEntity.cs
│   ├── UserEntity.cs
│   └── EmployeeEntity.cs
├── DI/
│   ├── Customer/ICustomerRepo.cs
│   ├── Product/IProductRepo.cs
│   ├── Stock/IStockRepo.cs
│   ├── Inward/IInwardRepo.cs
│   ├── Outward/IOutwardRepo.cs
│   ├── Order/IOrderRepo.cs + IOrderItemRepo.cs
│   ├── Ledger/ILedgerRepo.cs
│   └── User/IUserRepo.cs
├── Repos/
│   └── IBaseRepo.cs            # Generic interface
├── Querys/
│   └── SubmitModel.cs
└── Business/                   # (placeholder)
```

Mỗi file entity có property **snake_case** để map trực tiếp với cột DB (xem [Quy tắc snake_case](#6-quy-tắc-snake_case--pascalcase)).

### 3.2 `BE.Domain.Mysql/`

Chứa implementation của repo, dùng **Dapper + MySqlConnector**.

```
BE.Domain.Mysql/
├── BaseRepo.cs                 # Base class cho repo
├── DapperRepo.cs               # Dapper wrapper
├── CustomerRepo.cs
├── ProductRepo.cs
├── StockRepo.cs
├── InwardRepo.cs
├── OutwardRepo.cs
├── OrderRepo.cs
├── OrderItemRepo.cs
├── LedgerRepo.cs
└── UserRepo.cs
```

Mỗi repo nhận `connectionString` qua constructor, dùng `using var conn = new MySqlConnection(_connectionString)` cho mỗi thao tác.

### 3.3 `BE.Application.Contracts/`

Chứa **interface + DTO** cho service layer.

```
BE.Application.Contracts/
├── Dtos/
│   ├── Customer/ (CustomerDto, CustomerCreateDto, CustomerUpdateDto)
│   ├── Product/
│   ├── Stock/
│   ├── Inward/
│   ├── Outward/
│   ├── Order/ (OrderDto, OrderCreateDto, OrderItemDto, OrderItemCreateDto)
│   ├── PagingFilterDto.cs
│   └── PagingResult.cs
├── Interfaces/
│   ├── Customer/ICustomerService.cs
│   ├── Product/IProductService.cs
│   ├── Stock/IStockService.cs
│   ├── Inward/IInwardService.cs
│   ├── Outward/IOutwardService.cs
│   ├── Order/IOrderService.cs
│   ├── Ledger/ILedgerService.cs
│   ├── User/IAuthService.cs
│   └── IBusinessException.cs
└── Exceptions/
```

### 3.4 `BE.Application/`

Chứa **implementation** cho service.

```
BE.Application/
├── Services/
│   ├── Customer/CustomerService.cs
│   ├── Product/ProductService.cs
│   ├── Stock/StockService.cs
│   ├── Inward/InwardService.cs
│   ├── Outward/OutwardService.cs
│   └── Order/OrderService.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs    # AddApplication() helper
├── Exceptions/
│   └── BusinessException.cs
└── Class1.cs                  # placeholder
```

### 3.5 `BE.HostBase/`

Shared extensions cho host project.

```
BE.HostBase/
└── Extensions/
    └── (các helper DI, JWT config, NLog config)
```

Hiện tại ít được dùng — hầu hết host code copy-paste NLog + JWT config trong `Program.cs`.

### 3.6 `Workers/Workers.Shared/`

Shared library cho 2 worker.

```
Workers.Shared/
├── Models/
│   ├── OrderCreatedMessage.cs
│   └── LedgerChangeMessage.cs
├── Services/
│   ├── IKafkaProducerService.cs + KafkaProducerService.cs
│   └── KafkaConsumerBase.cs   # Abstract base cho consumer
```

`KafkaConsumerBase` chứa boilerplate subscribe/consume loop. `LedgerWorker` kế thừa trực tiếp; `VoucherWorker` tự build inline (chưa refactor).

## 4. Pattern đang dùng

### 4.1 Repository + Service

Cổ điển 2-layer:

```csharp
// Controller
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;
    public CustomersController(ICustomerService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());
}

// Service
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepo _repo;
    public CustomerService(ICustomerRepo repo) => _repo = repo;

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var entities = await _repo.GetAllAsync();
        return entities.Select(MapToDto);
    }
}

// Repo
public class CustomerRepo : ICustomerRepo
{
    public async Task<IEnumerable<CustomerEntity>> GetAllAsync() { /* Dapper */ }
}
```

### 4.2 Event-driven (Kafka)

Service publish message qua `IKafkaProducerService` (Singleton):

```csharp
public class OutwardService : IOutwardService
{
    private readonly IKafkaProducerService _kafkaProducer;

    public async Task<OutwardDto> CreateAsync(OutwardCreateDto dto)
    {
        // 1. INSERT outward vào DB
        var outward = new OutwardEntity { /* ... */ };
        await _outwardRepo.InsertAsync(outward);

        // 2. Publish ledger-change
        var msg = new LedgerChangeMessage
        {
            voucher_id = outward.outward_id.ToString(),
            voucher_type = "OUTWARD",
            product_id = outward.product_id.ToString(),
            stock_id = outward.stock_id.ToString(),
            quantity = outward.quantity,
            event_type = "CREATE",
            timestamp = DateTime.UtcNow.ToString("o")
        };
        await _kafkaProducer.ProduceAsync("ledger-change", msg.voucher_id, JsonSerializer.Serialize(msg));

        return MapToDto(outward);
    }
}
```

### 4.3 Dependency Injection lifetime

| Layer | Lifetime |
|---|---|
| Repository (DapperRepo, CustomerRepo, ...) | **Scoped** (mỗi request tạo mới connection) |
| Service (CustomerService, OrderService, ...) | **Scoped** |
| `IKafkaProducerService` | **Singleton** (giữ connection lâu dài) |
| `IConfiguration` | Singleton (built-in) |
| `ILogger<T>` | Singleton (built-in) |

Lý do: Dapper connection cần tạo mới mỗi request (tránh race condition + memory leak). Kafka producer thì stateless và tốn kém để tạo → giữ singleton.

### 4.4 Logging (NLog)

Mọi service dùng cùng config NLog (copy-paste trong `Program.cs`):

```csharp
var config = new LoggingConfiguration();
var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDirectory);

var fileTarget = new FileTarget("logfile")
{
    FileName = Path.Combine(logDirectory, "${shortdate}.log"),
    Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message} | ${exception:format=tostring}"
};
var consoleTarget = new ConsoleTarget("logconsole") { /* tương tự */ };
config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);
config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, consoleTarget);
LogManager.Configuration = config;
```

Log ghi vào `bin/Debug/net10.0/logs/{date}.log` (local) hoặc stdout (Docker/k8s — Filebeat sẽ thu stdout).

## 5. Naming convention

### 5.1 PascalCase — class, method, property trong C#

```csharp
public class OrderService : IOrderService
{
    public async Task<OrderDto> CreateAsync(OrderCreateDto dto) { /* ... */ }
}
```

### 5.2 snake_case — property mapping với DB column, DTO, JSON message

```csharp
public class OrderEntity
{
    public Guid order_id { get; set; }      // ← snake_case, map với cột order_id
    public Guid customer_id { get; set; }
    public string order_code { get; set; }
}

public class OrderCreateDto
{
    public Guid customer_id { get; set; }   // ← snake_case trong JSON request
    public Guid stock_id { get; set; }
    public List<OrderItemCreateDto> items { get; set; }
}

public class OrderCreatedMessage    // Kafka message
{
    public string order_id { get; set; }
    public string customer_id { get; set; }
    // ...
}
```

### 5.3 Quy tắc khác

- **Controller**: `EntityController` (vd: `OrdersController`)
- **Service**: `EntityService` implement `IEntityService`
- **Repo**: `EntityRepo` implement `IEntityRepo`
- **DI folder** (cho interface repo): `BE.Domain/DI/Entity/`
- **Routes**: `[Route("api/[controller]")]` cho Business/Order, `[Route("[controller]")]` cho Auth
- **XML comment**: tiếng Việt có dấu, tất cả method public

## 6. Quy tắc XML comment tiếng Việt

Mọi method public đều có XML doc comment tiếng Việt:

```csharp
/// <summary>
/// Tạo đơn hàng mới (tự động publish Kafka message)
/// </summary>
/// <param name="dto">Thông tin đơn hàng</param>
/// <returns>Đơn hàng vừa tạo</returns>
[HttpPost]
public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
{
    // ...
}
```

## 7. Build & run

### 7.1 Build solution

```bash
cd backend
dotnet build Ecom.slnx
```

### 7.2 Run từng service

Mỗi service trong 1 terminal riêng:

```bash
# Terminal 1: AuthApi
dotnet run --project AuthApi/AuthApi.csproj
# → http://localhost:5289

# Terminal 2: BusinessApi
dotnet run --project BusinessApi/BusinessApi.csproj
# → http://localhost:5119

# Terminal 3: OrderApi
dotnet run --project OrderApi/OrderApi.csproj
# → http://localhost:5120

# Terminal 4: LedgerWorker
dotnet run --project Workers/LedgerWorker/LedgerWorker.csproj

# Terminal 5: VoucherWorker
dotnet run --project Workers/VoucherWorker/VoucherWorker.csproj

# Terminal 6: ApiGateway
dotnet run --project ApiGateway/ApiGateway.csproj
# → http://localhost:5000
```

Default ports (từ `Properties/launchSettings.json` của mỗi project) là `5289/5119/5120/5000` — KHÔNG phải `5001/5002/5003/5000` như docker-compose.

### 7.3 Swagger UI

Mỗi API service có Swagger UI ở `/swagger` (dev mode):
- AuthApi: `http://localhost:5289/swagger`
- BusinessApi: `http://localhost:5119/swagger`
- OrderApi: `http://localhost:5120/swagger`

## 8. Out of scope (chưa làm)

- ❌ Unit test / Integration test (chưa có project test)
- ❌ Benchmark / load test
- ❌ Code coverage
- ❌ SonarQube / static analysis
- ❌ Centralized exception handling middleware
- ❌ Health check chi tiết (DB connection, Kafka connection)
- ❌ Distributed tracing