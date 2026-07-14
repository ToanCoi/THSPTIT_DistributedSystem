# Kiến trúc dữ liệu

## Tổng quan

```
┌─────────────────────────────────────────────────────────────┐
│                                                              │
│   business_db (MySQL 8.0)                                    │
│   ┌──────────────────────────────────────────────────────┐   │
│   │ customers ─── orders ── order_items                  │   │
│   │                          │                            │   │
│   │ products ─── inwards     │                            │   │
│   │    │      │              │                            │   │
│   │    │      └── led_inventory_item_ledger              │   │
│   │    │             │                                    │   │
│   │    │             ├── led_inventory_item_ledger_date   │   │
│   │    │             │                                    │   │
│   │    └─────────────┴── led_inventory_item_ledger_closing│   │
│   │ stocks ─── outwards ──┘                               │   │
│   └──────────────────────────────────────────────────────┘   │
│                                                              │
│   master_db (MySQL 8.0)                                      │
│   ┌──────────────────────────────────────────────────────┐   │
│   │ users (id, username, password_hash, jwt, ...)        │   │
│   └──────────────────────────────────────────────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

Chi tiết schema từng bảng: [../01-business/data-model.md](../01-business/data-model.md).

## 1. MySQL — Database engine

- **Version**: 8.0 (image `mysql:8.0`)
- **Engine mặc định**: InnoDB (hỗ trợ transaction + row-level lock)
- **Character set**: utf8mb4 (đề xuất, kiểm tra trong init.sql nếu muốn chính thức)
- **Collation**: utf8mb4_unicode_ci

## 2. Tại sao KHÔNG dùng EF Core?

- Dùng **Dapper** (micro-ORM) thuần cho mọi truy vấn
- Lý do (theo quyết định thiết kế ban đầu):
  - Performance tốt hơn cho query đơn giản
  - SQL rõ ràng, dễ debug, dễ optimize
  - Tránh over-engineering cho project demo
- Nhược điểm: phải tự viết SQL, không có migration tự động

## 3. Repository pattern

### 3.1 Interface (`backend/BE.Domain/`)

```csharp
// IBaseRepo - generic CRUD
public interface IBaseRepo
{
    Task<PagingResult<T>> GetPaging<T>(string columns, int skip, int take, string sort, object? where);
    // + các method khác
}

// IRepo cho từng entity
public interface ICustomerRepo : IBaseRepo
{
    Task<CustomerEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<CustomerEntity>> GetAllAsync();
    Task InsertAsync(CustomerEntity entity);
    Task UpdateAsync(CustomerEntity entity);
    Task DeleteAsync(Guid id);
    // ...
}
```

### 3.2 Implementation (`backend/BE.Domain.Mysql/`)

```csharp
public class CustomerRepo : ICustomerRepo
{
    private readonly string _connectionString;

    public CustomerRepo(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CustomerEntity?> GetByIdAsync(Guid id)
    {
        using var conn = new MySqlConnection(_connectionString);
        var sql = "SELECT * FROM customers WHERE customer_id = @id";
        return await conn.QueryFirstOrDefaultAsync<CustomerEntity>(sql, new { id });
    }
    // ...
}
```

Mỗi entity có 1 repo file riêng trong `BE.Domain.Mysql/`:
- `CustomerRepo.cs`
- `ProductRepo.cs`
- `StockRepo.cs`
- `InwardRepo.cs`
- `OutwardRepo.cs`
- `OrderRepo.cs`
- `OrderItemRepo.cs`
- `LedgerRepo.cs`
- `UserRepo.cs`

### 3.3 Đăng ký DI

Mỗi API service đăng ký repo + interface trong `Program.cs`:

```csharp
// BusinessApi/Program.cs
builder.Services.AddScoped<IBaseRepo>(sp => new DapperRepo(connectionString));
builder.Services.AddScoped<ICustomerRepo>(sp => new CustomerRepo(connectionString));
builder.Services.AddScoped<IProductRepo>(sp => new ProductRepo(connectionString));
// ...
```

**Lưu ý**: `OrderApi/Program.cs:73` đăng ký `IOutwardRepo` — vi phạm ranh giới service (OrderApi không nên biết về Outward). Lý do: cần `IOutwardRepo` để cascade-delete order (xóa outward liên quan). Trong tương lai nên refactor: tạo endpoint riêng ở BusinessApi hoặc dùng event.

## 4. Connection string

### 4.1 Trong docker-compose

`backend/docker-compose.yml`:
- MySQL master: `Server=mysql-master;Database=master_db;User=root;Password=P@ssw0rd123;`
- MySQL business: `Server=mysql-business;Database=business_db;User=root;Password=P@ssw0rd123;`

### 4.2 Trong k8s (qua Helm values)

`infra/charts/ecom-stack/values.yaml`:
```yaml
global:
  localMysql:
    host: "host.minikube.internal"
    port: 3306
    user: "root"
    password: "Mysql!110720"   # Password trên Windows host, KHÁC docker-compose
    masterDb: "master_db"
    businessDb: "business_db"
```

MySQL **chạy ngoài cluster** trên Windows host (vì Bitnami bị gỡ khỏi Docker Hub late 2025). Pod trong cluster truy cập qua DNS đặc biệt `host.minikube.internal` (chỉ có ở Docker driver).

### 4.3 Khi dev với `dotnet run` (không qua docker)

Default trong code (mỗi `Program.cs`):
```csharp
"Server=localhost;Port=3306;Database=business_db;User=root;Password=Mysql!110720;"
```

→ Nghĩa là khi dev local phải có MySQL trên `localhost:3306` với password `Mysql!110720` (hoặc override qua env var `ConnectionStrings__BusinessConnection`).

### 4.4 Env var

| Service | Env var | Default |
|---|---|---|
| AuthApi | `ConnectionStrings__Default` | `Server=localhost;Port=3306;Database=master_db;User=root;Password=Mysql!110720;` |
| BusinessApi | `ConnectionStrings__BusinessConnection` | `Server=localhost;Port=3306;Database=business_db;User=root;Password=Mysql!110720;` |
| OrderApi | `ConnectionStrings__BusinessConnection` | (giống trên) |
| VoucherWorker | `ConnectionStrings__BusinessConnection` | (giống trên) |
| LedgerWorker | `ConnectionStrings__BusinessConnection` | (giống trên) |

## 5. Dapper — cú pháp cơ bản

### 5.1 Query đơn

```csharp
var sql = "SELECT * FROM customers WHERE customer_id = @id";
var customer = await conn.QueryFirstOrDefaultAsync<CustomerEntity>(sql, new { id });
```

### 5.2 Query nhiều

```csharp
var sql = "SELECT * FROM customers ORDER BY created_date DESC";
var customers = await conn.QueryAsync<CustomerEntity>(sql);
```

### 5.3 Insert

```csharp
var sql = @"INSERT INTO customers (customer_id, full_name, phone, email, address, created_date)
            VALUES (@customer_id, @full_name, @phone, @email, @address, @created_date)";
await conn.ExecuteAsync(sql, entity);
```

### 5.4 Update

```csharp
var sql = @"UPDATE customers SET full_name = @full_name, phone = @phone
            WHERE customer_id = @customer_id";
await conn.ExecuteAsync(sql, entity);
```

### 5.5 Paging

```csharp
var sql = @"SELECT * FROM customers
            ORDER BY @sort_field @sort_order
            LIMIT @take OFFSET @skip";
var countSql = "SELECT COUNT(*) FROM customers";
// ...
```

## 6. Khởi tạo database

### 6.1 Script

`backend/Scripts/init.sql` — schema + seed data (1 admin user, 1 customer, 1 stock, 1 product).

### 6.2 Chạy khi dev (docker compose)

File mount tự động vào `/docker-entrypoint-initdb.d/init.sql` → MySQL container tự chạy khi khởi động lần đầu.

### 6.3 Chạy khi k8s

Script `infra/scripts/seed-mysql.sh` chạy sau khi deploy:
```bash
./infra/scripts/seed-mysql.sh
```

Script này exec vào MySQL trên Windows host (không phải trong cluster) và import init.sql.

### 6.4 Chạy thủ công

```bash
# Local
mysql -h localhost -u root -p business_db < backend/Scripts/init.sql

# Docker container
docker exec -i ecom-mysql-master mysql -uroot -pP@ssw0rd123 master_db < backend/Scripts/init.sql
docker exec -i ecom-mysql-business mysql -uroot -pP@ssw0rd123 business_db < backend/Scripts/init.sql
```

## 7. Migrations

Hệ thống chưa có migration tool tự động (do không dùng EF Core). Migrations quản lý thủ công qua file SQL:

- `backend/Scripts/migration_add_stock_and_sequence.sql` — thêm `stock_id` cho orders + sequence cho order_code
- `backend/Scripts/rebuild_closing.sql` — rebuild closing từ ledger (chạy khi cần tính lại tồn kho)

Cách chạy:
```bash
mysql -h localhost -u root -p business_db < backend/Scripts/migration_add_stock_and_sequence.sql
```

## 8. Transaction & concurrency

### 8.1 Generate order code (atomic)

`OrderRepo.GetNextOrderCodeAsync` — dùng `SELECT ... FOR UPDATE` với row lock để đảm bảo sequence tăng dần khi nhiều request đồng thời.

```sql
START TRANSACTION;
SELECT next_val FROM order_sequence WHERE id = 1 FOR UPDATE;
UPDATE order_sequence SET next_val = next_val + 1 WHERE id = 1;
COMMIT;
```

### 8.2 Cascade delete order

Trong `OrderService.RemoveAsync`:
1. Lấy danh sách outward theo `order_id`
2. Với mỗi outward: publish Kafka (async)
3. Xóa outwards, order_items, order (DB transaction)

**Race condition tiềm ẩn**: giữa lúc publish Kafka và xóa DB, có thể có request khác đọc outward cũ. Nếu cần strict consistency, nên dùng **outbox pattern** (chưa implement).

## 9. Backup & restore

Hiện tại **chưa có backup tự động**. Khi chạy local, dữ liệu lưu trong Docker volume:

```bash
# Backup
docker exec ecom-mysql-business mysqldump -uroot -pP@ssw0rd123 business_db > backup.sql

# Restore
docker exec -i ecom-mysql-business mysql -uroot -pP@ssw0rd123 business_db < backup.sql
```

Khi chạy k8s, MySQL trên Windows host → dùng `mysqldump` trực tiếp:
```bash
mysqldump -h host.minikube.internal -u root -pMysql!110720 business_db > backup.sql
```

## 10. Out of scope (chưa làm)

- ❌ Read replica
- ❌ Sharding
- ❌ Multi-tenant database per customer
- ❌ Automated backup
- ❌ Query performance monitoring (chỉ có NLog)
- ❌ Database migration tool (Flyway, Liquibase)