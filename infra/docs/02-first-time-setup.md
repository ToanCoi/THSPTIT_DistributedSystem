# Hướng dẫn cài đặt & chạy lần đầu

Hướng dẫn này dành cho máy **chưa có gì** (mới cài OS, format, hoặc máy mới). Nếu máy đã có minikube + Docker rồi, xem [01-deploy-scripts.md](01-deploy-scripts.md) để biết bước nào cần chạy.

## 1. Yêu cầu phần cứng

| Tài nguyên | Tối thiểu | Khuyến nghị |
|---|---|---|
| CPU | 4 cores | 6+ cores |
| RAM | 8 GB | 16 GB |
| Disk trống | 30 GB | 50 GB+ |
| OS | Windows 10/11, macOS 12+, Ubuntu 20.04+ | – |

Máy < 8 GB RAM: chỉnh `infra/logging/elasticsearch-values.yaml` giảm heap xuống 256m.

## 2. Cài đặt công cụ

### Windows (khuyến nghị dùng Chocolatey)

Mở **PowerShell Admin**, cài Chocolatey trước (nếu chưa có):

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
```

Cài tất cả tool cần thiết:

```powershell
choco install -y docker-desktop minikube kubernetes-cli kubernetes-helm jq git
```

> Sau khi cài xong, **đóng và mở lại Git Bash** để PATH được cập nhật.

### macOS

```bash
brew install --cask docker minikube
brew install kubernetes-cli helm git jq
```

### Ubuntu/Debian

```bash
# Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# Sau đó logout/login lại
# minikube
curl -LO https://storage.googleapis.com/minikube/releases/latest/docker-machine-driver-kvm2
# ... hoặc dùng driver=docker
# kubectl, helm, jq
sudo apt install -y kubectl helm jq git
```

## 3. Cấu hình Docker Desktop

### Windows / macOS

- Mở Docker Desktop → Settings → Resources:
  - CPUs ≥ 4
  - Memory ≥ 8 GB
  - Disk ≥ 30 GB
- Settings → WSL Integration (Windows): bật

### Verify môi trường

Mở Git Bash, chạy lần lượt (mỗi lệnh phải in ra version, không phải "command not found"):

```bash
docker --version
minikube version
kubectl version --client
helm version
jq --version
```

## 4. Clone repo và chạy first-time

```bash
cd <đường-dẫn-muốn-chứa-project>
git clone <repo-url> THSPTIT_DistributedSystem
cd THSPTIT_DistributedSystem
```

Chạy master script (làm tất cả theo thứ tự):

```bash
./infra/scripts/all.sh
```

Script sẽ hỏi `Seed MySQL data? [y/N]` ở cuối — gõ `y` nếu muốn có data mẫu.

**Thời gian**: ~10-20 phút tuỳ máy (build images chiếm phần lớn).

## 5. Sau khi `all.sh` xong

Mở **terminal Git Bash MỚI** (giữ nguyên terminal hiện tại), chạy:

```bash
minikube -p ecom tunnel
```

> Terminal này sẽ block (không return). **Cứ để nguyên**, mở terminal mới cho các lệnh khác.

## 6. Cấu hình hosts (chỉ làm 1 lần trên Windows)

Mở **PowerShell Admin** rồi chạy:

```powershell
Add-Content C:\Windows\System32\drivers\etc\hosts "127.0.0.1 ecom.local"
ipconfig /flushdns
```

Verify:

```powershell
Select-String "ecom.local" C:\Windows\System32\drivers\etc\hosts
```

Phải in ra dòng `127.0.0.1 ecom.local` là OK.

## 7. Truy cập ứng dụng

Mở browser: **http://ecom.local/**

Hoặc mở **K8s dashboard**: terminal khác, chạy `minikube -p ecom dashboard` (cũng blocking).

## 8. Verify nhanh

```bash
# 16 pods đều Running
kubectl -n ecom get pods

# Frontend OK
curl -sI http://ecom.local/   # expect HTTP 200

# API gateway hoạt động (sau khi fix bug API_BASE_URL)
curl -sI http://ecom.local/Auth/health
```

## Tiếp theo

- Mỗi lần restart máy: xem [03-daily-restart.md](03-daily-restart.md)
- Cần biết URLs/services/architecture: xem [04-system-overview.md](04-system-overview.md)
- Gặp lỗi: xem [05-troubleshooting.md](05-troubleshooting.md)
