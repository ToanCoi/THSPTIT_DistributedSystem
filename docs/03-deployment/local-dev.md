# Build & run code local

Hướng dẫn build và chạy hệ thống trên máy local, **không qua Kubernetes**. Có 3 option từ nhanh đến custom.

## 1. Docker Compose (khuyến nghị cho dev)

### 1.1 Cần gì

- Docker Desktop
- Node.js 20+ (cho frontend)
- ~5 phút

### 1.2 Khởi động

```bash
cd backend
docker compose up -d
```

Lệnh này khởi động **9 container**:
- 2 MySQL (master + business)
- 1 Zookeeper
- 1 Kafka broker
- 4 API (api-gateway, auth-api, business-api, order-api)
- 2 worker (voucher-worker, ledger-worker)

### 1.3 Kiểm tra

```bash
# Containers đang chạy
docker ps

# Đợi MySQL ready (~30s)
docker logs ecom-mysql-master --tail 10
docker logs ecom-mysql-business --tail 10

# Test API gateway
curl http://localhost:5000/health
# expect: {"status":"ok","service":"ApiGateway"}

# Test login (sau khi MySQL ready)
curl -X POST http://localhost:5000/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

### 1.4 Chạy frontend

Mở terminal mới:

```bash
cd frontend
npm install
npm run dev
```

Mở browser: **http://localhost:3000**

Đăng nhập với `admin` / `admin123`.

### 1.5 Xem log

```bash
# Tất cả log
docker compose logs -f

# Log 1 service cụ thể
docker logs ecom-business-api -f --tail 50

# Worker log (Kafka consumer)
docker logs ecom-ledger-worker -f --tail 50
docker logs ecom-voucher-worker -f --tail 50
```

### 1.6 Dừng

```bash
# Dừng + xóa container (giữ volume)
docker compose down

# Dừng + xóa container + xóa volume (sạch DB)
docker compose down -v
```

### 1.7 Kết nối MySQL từ máy host

```bash
# MySQL master (port 3306)
mysql -h localhost -P 3306 -u root -pP@ssw0rd123 master_db

# MySQL business (port 3307)
mysql -h localhost -P 3307 -u root -pP@ssw0rd123 business_db
```

---

## 2. dotnet run + npm run dev (debug code)

Phù hợp khi muốn debug code C# (đặt breakpoint, edit & reload) hoặc Vue (hot reload).

### 2.1 Cần gì

- .NET 10 SDK
- Docker Desktop (chỉ để chạy MySQL + Kafka)
- Node.js 20+
- ~10 phút

### 2.2 Bước 1 — Khởi động MySQL + Kafka qua Docker

`docker-compose.yml` chỉ định nghĩa 9 service, nhưng có thể chạy 1 phần bằng cách filter:

```bash
cd backend

# Chỉ chạy MySQL + Kafka (bỏ qua 4 API + 2 worker)
docker compose up -d mysql-master mysql-business zookeeper kafka

# Hoặc dùng docker run trực tiếp cho MySQL
docker run -d --name mysql-master \
  -e MYSQL_ROOT_PASSWORD=Mysql!110720 \
  -e MYSQL_DATABASE=master_db \
  -p 3306:3306 \
  -v ${PWD}/Scripts:/docker-entrypoint-initdb.d \
  mysql:8.0

docker run -d --name mysql-business \
  -e MYSQL_ROOT_PASSWORD=Mysql!110720 \
  -e MYSQL_DATABASE=business_db \
  -p 3306:3306 \
  -v ${PWD}/Scripts:/docker-entrypoint-initdb.d \
  mysql:8.0

# Kafka + Zookeeper (dùng docker compose để tiện)
docker compose up -d zookeeper kafka
```

### 2.3 Bước 2 — Run từng API service qua dotnet

Mỗi service trong 1 terminal riêng:

```bash
# Terminal 1: AuthApi → http://localhost:5289
cd backend
dotnet run --project AuthApi/AuthApi.csproj

# Terminal 2: BusinessApi → http://localhost:5119
dotnet run --project BusinessApi/BusinessApi.csproj

# Terminal 3: OrderApi → http://localhost:5120
dotnet run --project OrderApi/OrderApi.csproj

# Terminal 4: LedgerWorker (background)
dotnet run --project Workers/LedgerWorker/LedgerWorker.csproj

# Terminal 5: VoucherWorker (background)
dotnet run --project Workers/VoucherWorker/VedgerWorker.csproj

# Terminal 6: ApiGateway → http://localhost:5000
dotnet run --project ApiGateway/ApiGateway.csproj
```

### 2.4 Bước 3 — Run frontend

Terminal 7:
```bash
cd frontend
npm install
npm run dev
```

Mở http://localhost:3000.

### 2.5 Lưu ý — Default ports khi `dotnet run`

| Service | Port khi `dotnet run` | Port khi docker compose |
|---|---|---|
| AuthApi | 5289 | 5001 |
| BusinessApi | 5119 | 5002 |
| OrderApi | 5120 | 5003 |
| ApiGateway | 5000 | 5000 |

Khi dev với `dotnet run`, ApiGateway đọc env var (default trong code):
- `Services__AuthApi = http://localhost:5289` ✓
- `Services__BusinessApi = http://localhost:5119` ✓
- `Services__OrderApi = http://localhost:5120` ✓

Nếu port khác, override qua env:
```bash
# Windows (PowerShell)
$env:Services__AuthApi = "http://localhost:5001"
dotnet run --project ApiGateway/ApiGateway.csproj

# macOS/Linux (bash)
Services__AuthApi=http://localhost:5001 dotnet run --project ApiGateway/ApiGateway.csproj
```

### 2.6 Lưu ý — Vite proxy trỏ vào đúng port

`frontend/vite.config.js` mặc định proxy `/Auth`, `/Business`, `/Order` → `http://localhost:62739`. Đây là port ASP.NET mặc định, **không phải port ApiGateway**.

Nếu muốn frontend gọi qua ApiGateway (port 5000), sửa `vite.config.js`:

```js
proxy: {
  '/Auth': 'http://localhost:5000',
  '/Business': 'http://localhost:5000',
  '/Order': 'http://localhost:5000'
}
```

Restart Vite (`npm run dev`) sau khi sửa.

### 2.7 Lưu ý — Connection string khi dev

Default trong code mỗi `Program.cs`:
```csharp
"Server=localhost;Port=3306;Database=business_db;User=root;Password=Mysql!110720;"
```

Nếu MySQL chạy với password khác, override:
```bash
# PowerShell
$env:ConnectionStrings__BusinessConnection = "Server=localhost;Port=3306;Database=business_db;User=root;Password=YOUR_PASSWORD;"
dotnet run --project BusinessApi/BusinessApi.csproj
```

### 2.8 Log

Mỗi service ghi log ra:
- File: `backend/{Service}/bin/Debug/net10.0/logs/{shortdate}.log`
- Console: stdout

```bash
# Xem log realtime
tail -f backend/BusinessApi/bin/Debug/net10.0/logs/2026-07-14.log
```

---

## 3. Từng service riêng (Swagger test)

Phù hợp khi chỉ muốn test 1 service qua Swagger UI, không cần full stack.

### 3.1 Chạy 1 service + MySQL

```bash
# 1. Khởi động MySQL + Kafka (chỉ cần 2 container)
cd backend
docker compose up -d mysql-business zookeeper kafka

# 2. Chạy BusinessApi
cd BusinessApi
dotnet run
# → http://localhost:5119/swagger
```

Swagger UI mở tại `http://localhost:5119/swagger` — có thể test tất cả endpoint BusinessApi ngay.

### 3.2 Test qua Postman / curl

```bash
# Lấy token
TOKEN=$(curl -s -X POST http://localhost:5000/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' \
  | jq -r '.token')

# Gọi API có auth
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/Business/api/customers
```

### 3.3 Test event-driven flow

```bash
# Xem message trong Kafka topic ledger-change
docker exec -it ecom-kafka /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 \
  --topic ledger-change \
  --from-beginning

# Terminal khác: tạo 1 phiếu nhập
curl -X POST http://localhost:5000/Business/api/inwards \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "product_id": "d4e5f6a7-b8c9-0123-def0-456789012345",
    "stock_id": "c3d4e5f6-a7b8-9012-cdef-345678901234",
    "quantity": 100,
    "unit_price": 50000,
    "invoice_date": "2026-07-14"
  }'

# → Terminal consumer sẽ in ra message ledger-change
# → Worker log sẽ in ra "Processed inward ..."
# → MySQL: SELECT * FROM led_inventory_item_ledger; có 1 row mới
```

---

## 4. Debug Kafka message

### 4.1 List topic

```bash
docker exec -it ecom-kafka /opt/kafka/bin/kafka-topics.sh \
  --bootstrap-server localhost:9092 --list
```

### 4.2 Xem message trong topic

```bash
# order-created
docker exec -it ecom-kafka /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 \
  --topic order-created \
  --from-beginning

# ledger-change
docker exec -it ecom-kafka /opt/kafka/bin/kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 \
  --topic ledger-change \
  --from-beginning
```

### 4.3 Xem message offset

```bash
docker exec -it ecom-kafka /opt/kafka/bin/kafka-consumer-groups.sh \
  --bootstrap-server localhost:9092 --list

docker exec -it ecom-kafka /opt/kafka/bin/kafka-consumer-groups.sh \
  --bootstrap-server localhost:9092 \
  --describe --group ledger-worker-group
```

### 4.4 Tool trực quan (tuỳ chọn)

- **Offset Explorer** (free) — GUI đẹp, [kafkatool.com](https://www.kafkatool.com/)
- **Conduktor** (có bản free trial)
- Kết nối: `localhost:9093`

---

## 5. Xử lý sự cố

### 5.1 Container không start

```bash
docker compose logs <service-name>
# Xem lỗi cụ thể
```

Lỗi thường gặp:
- **Port đã bị chiếm** → đổi port trong `docker-compose.yml`
- **MySQL không healthy** → đợi 30s cho MySQL init

### 5.2 `dotnet run` lỗi "Unable to connect to MySQL"

Kiểm tra:
```bash
# MySQL có chạy không?
docker ps | grep mysql

# Test connection
mysql -h localhost -P 3306 -u root -pMysql!110720 -e "SHOW DATABASES"
```

### 5.3 `dotnet run` lỗi "Unable to connect to Kafka"

```bash
# Kafka có chạy không?
docker ps | grep kafka

# Test connection
docker exec -it ecom-kafka /opt/kafka/bin/kafka-broker-api-versions.sh \
  --bootstrap-server localhost:9092
```

### 5.4 Frontend không gọi được API

- Kiểm tra console browser (F12)
- Kiểm tra Network tab xem request đi đâu
- Verify Vite proxy config trong `vite.config.js`

### 5.5 Worker không nhận message

- Xem worker log: `docker logs ecom-ledger-worker --tail 50`
- Xem topic có message không (mục 4.2)
- Verify `Kafka:BootstrapServers` env đúng (default `localhost:9093`)

### 5.6 Reset tất cả

```bash
cd backend
docker compose down -v   # Xóa container + volume
docker compose up -d
cd ../frontend
rm -rf node_modules
npm install
```