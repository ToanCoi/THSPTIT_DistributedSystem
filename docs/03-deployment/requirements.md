# Yêu cầu phần cứng & công cụ

Tài liệu này liệt kê **yêu cầu phần cứng** và **công cụ cần cài** cho từng kịch bản triển khai.

## 1. Yêu cầu phần cứng

### Tối thiểu

| Tài nguyên | Yêu cầu |
|---|---|
| CPU | 4 cores |
| RAM | 8 GB |
| Disk trống | 30 GB |
| OS | Windows 10/11, macOS 12+, Ubuntu 20.04+ |

### Khuyến nghị (cho k8s local)

| Tài nguyên | Yêu cầu |
|---|---|
| CPU | 6+ cores |
| RAM | 16 GB |
| Disk trọng | 50 GB+ |

Nếu máy < 8 GB RAM và chạy k8s + ELK, cần giảm Elasticsearch heap (xem [k8s-deploy.md § Troubleshooting](k8s-deploy.md#troubleshooting)).

## 2. Công cụ theo kịch bản

### Kịch bản 1 — Docker Compose

| Công cụ | Version | Bắt buộc? |
|---|---|---|
| Docker Desktop | 4.x+ | ✅ Bắt buộc |
| Node.js | 20.x+ | ✅ Bắt buộc (cho frontend dev) |
| npm | 10.x+ | ✅ Bắt buộc |

Không cần cài .NET SDK, MySQL, Kafka — tất cả chạy trong container.

### Kịch bản 2 — dotnet run + npm run dev

| Công cụ | Version | Bắt buộc? |
|---|---|---|
| .NET SDK | 10.0 | ✅ Bắt buộc |
| Docker Desktop | 4.x+ | ✅ Bắt buộc (chạy MySQL + Kafka qua container) |
| Node.js | 20.x+ | ✅ Bắt buộc |
| npm | 10.x+ | ✅ Bắt buộc |
| MySQL client | 8.0+ | Khuyến nghị (để debug SQL) |
| Kafka CLI | bất kỳ | Tuỳ chọn (để debug Kafka message) |

### Kịch bản 3 — Kubernetes local

| Công cụ | Version | Bắt buộc? |
|---|---|---|
| Docker Desktop | 4.x+ | ✅ Bắt buộc (driver cho minikube) |
| minikube | latest | ✅ Bắt buộc |
| kubectl | 1.28+ | ✅ Bắt buộc |
| Helm | 3.x | ✅ Bắt buộc |
| jq | 1.6+ | ✅ Bắt buộc (script bash dùng parse JSON) |
| Git Bash | 4.x+ | Khuyến nghị (Windows) |
| MySQL client | 8.0+ | Khuyến nghị |

Ngoài ra cần **MySQL Server** chạy ngoài cluster (trên Windows host) vì project dùng `host.minikube.internal:3306`. Có 2 cách:

1. **Cài MySQL Server trên Windows** (khuyến nghị): download từ [mysql.com](https://dev.mysql.com/downloads/mysql/), password `Mysql!110720`.
2. **Chạy MySQL qua Docker Desktop**: `docker run -d -p 3306:3306 -e MYSQL_ROOT_PASSWORD=Mysql!110720 mysql:8.0` (nhưng cần expose đúng cách).

## 3. Cài đặt công cụ

### Windows (khuyến nghị dùng Chocolatey)

Mở **PowerShell Admin**, cài Chocolatey trước (nếu chưa có):

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
```

Cài tất cả công cụ cần thiết cho **cả 3 kịch bản**:

```powershell
# Docker Compose + k8s
choco install -y docker-desktop minikube kubernetes-cli kubernetes-helm jq git

# Backend dev
choco install -y dotnet-10.0-sdk

# Frontend dev
choco install -y nodejs-lts

# (Optional) Debug tools
choco install -y mysql.workbench
```

> Sau khi cài xong, **đóng và mở lại Git Bash** để PATH được cập nhật.

### macOS

```bash
# Docker + minikube
brew install --cask docker minikube

# Công cụ còn lại
brew install kubernetes-cli helm git jq dotnet node mysql-client
```

### Ubuntu / Debian

```bash
# Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# Logout/login lại sau bước này

# minikube
curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
sudo install minikube-linux-amd64 /usr/local/bin/minikube

# kubectl, helm, jq, git
sudo apt install -y kubectl helm jq git

# .NET 10 SDK
# (tham khảo https://learn.microsoft.com/dotnet/core/install/linux)

# Node 20+
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt install -y nodejs
```

## 4. Cấu hình Docker Desktop

### Windows / macOS

Mở Docker Desktop → Settings → Resources:
- **CPUs**: ≥ 4 (khuyến nghị 6+)
- **Memory**: ≥ 8 GB (khuyến nghị 12 GB cho k8s)
- **Disk**: ≥ 30 GB (khuyến nghị 50 GB+)
- **WSL Integration** (Windows): bật nếu dùng WSL

### Verify môi trường

Mở Git Bash, chạy lần lượt — mỗi lệnh phải in ra version (không phải `command not found`):

```bash
docker --version
# Docker version 24.x.x

dotnet --version
# 10.0.x

node --version
# v20.x.x

npm --version
# 10.x.x

minikube version
# minikube version: v1.32.x

kubectl version --client
# Client Version: v1.28.x

helm version
# version.BuildInfo{Version:"v3.14.x", ...}

jq --version
# jq-1.7.x

git --version
# git version 2.43.x
```

## 5. Yêu cầu đặc biệt cho Windows

### Hyper-V hoặc WSL2

Docker Desktop trên Windows cần 1 trong 2:
- **WSL 2** (khuyến nghị) — cài WSL + Ubuntu từ Microsoft Store
- **Hyper-V** (cũ hơn)

minikube dùng driver `docker` (mặc định), hoạt động trên cả WSL 2 và Hyper-V.

### PowerShell Admin

Một số thao tác cần PowerShell Admin (chỉ làm 1 lần):
- Sửa file `C:\Windows\System32\drivers\etc\hosts` (thêm `127.0.0.1 ecom.local`)
- Cài Chocolatey

### Git Bash quirks

Có một số quirk khi dùng Git Bash trên Windows với minikube/kubectl — xem [k8s-deploy.md § Troubleshooting Git Bash](k8s-deploy.md#git-bash--minikube-quirks).

## 6. Network ports cần mở

Mô tả ports cần truy cập:

### Local dev (Kịch bản 1, 2)

| Port | Service | Protocol |
|---|---|---|
| 3000 | Frontend (Vite dev) | HTTP |
| 5000 | ApiGateway | HTTP |
| 5001 | AuthApi (docker compose) / 5289 (dotnet run) | HTTP |
| 5002 | BusinessApi (docker compose) / 5119 (dotnet run) | HTTP |
| 5003 | OrderApi (docker compose) / 5120 (dotnet run) | HTTP |
| 3306 | MySQL master | MySQL |
| 3307 | MySQL business (docker compose only) | MySQL |
| 9092 | Kafka broker (internal) | TCP |
| 9093 | Kafka host (từ docker host vào container) | TCP |

### Kubernetes (Kịch bản 3)

| Port | Service | Protocol |
|---|---|---|
| 80 | Frontend (ingress) | HTTP |
| 80 | ApiGateway (ingress) | HTTP |
| 80 | Kibana (ingress `/kibana`) | HTTP |
| 3306 | MySQL trên Windows host | MySQL |
| 9092 | Kafka (internal cluster) | TCP |
| 9093 | Kafka (host port) | TCP |

## 7. Tiền kiểm tra nhanh

Chạy lệnh này để verify môi trường trước khi bắt đầu:

**Windows (PowerShell):**
```powershell
# Docker
docker --version
docker ps

# Minikube (sau khi cài)
minikube version

# .NET
dotnet --version

# Node
node --version

# Hypervisor enabled
(Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V).State
# → Should be "Enabled"
```

**macOS / Linux:**
```bash
docker --version
docker ps
dotnet --version
node --version
minikube version
```

Nếu tất cả OK → tiếp tục [local-dev.md](local-dev.md) hoặc [k8s-deploy.md](k8s-deploy.md).