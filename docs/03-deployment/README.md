# 03 — Triển khai

Phần này hướng dẫn **build và chạy** hệ thống ở các môi trường khác nhau.

| File | Nội dung |
|---|---|
| [requirements.md](requirements.md) | Yêu cầu phần cứng + công cụ cần cài (cho cả local dev + k8s) |
| [local-dev.md](local-dev.md) | Build & run code local (3 option: docker compose / dotnet run / từng service) |
| [k8s-deploy.md](k8s-deploy.md) | Dùng scripts build Kubernetes local trên minikube (đầy đủ tính năng) |

## 3 kịch bản triển khai

| Kịch bản | Mục đích | Thời gian setup | Độ phức tạp | File |
|---|---|---|---|---|
| **1. Dev local với Docker Compose** | Phát triển nhanh, test ngay | ~5 phút | Thấp | `backend/docker-compose.yml` |
| **2. Dev local với dotnet run + npm run dev** | Debug code C#/Vue | ~10 phút | Trung bình | `backend/{Service}/Program.cs` |
| **3. Kubernetes local trên minikube** | Demo môi trường production-like | ~15-20 phút | Cao | `infra/scripts/all.sh` |

### Khi nào dùng kịch bản nào?

- **Sửa code backend nhỏ** (sửa entity, thêm field): Kịch bản 2 (dotnet run) — debug nhanh nhất
- **Sửa code frontend**: Kịch bản 2 — npm run dev có hot reload
- **Test event-driven flow** (Kafka message): Kịch bản 1 (docker compose) — đủ worker, có log
- **Demo cho giảng viên / đồ án**: Kịch bản 3 (k8s) — đầy đủ tính năng phân tán, có ingress, có ELK
- **Test deployment, ingress, scale**: Kịch bản 3

## Quick start

### Kịch bản 1 — Docker Compose (nhanh nhất)

```bash
cd backend
docker compose up -d
# Đợi ~2 phút cho MySQL + Kafka + 6 service start

cd ../frontend
npm install
npm run dev
# Mở http://localhost:3000
```

Chi tiết: [local-dev.md §1](local-dev.md#1-docker-compose-khuyến-nghị-cho-dev)

### Kịch bản 3 — Kubernetes local (đầy đủ)

```bash
./infra/scripts/all.sh
# Mở terminal MỚI:
minikube -p ecom tunnel
# PowerShell Admin (1 lần):
Add-Content C:\Windows\System32\drivers\etc\hosts "127.0.0.1 ecom.local"
# Mở http://ecom.local/
```

Chi tiết: [k8s-deploy.md §1](k8s-deploy.md#1-quick-start-1-lệnh)