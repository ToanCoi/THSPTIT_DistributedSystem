# Hướng dẫn mỗi lần mở lại máy

Sau khi restart PC, minikube cluster vẫn còn (data persistent trong PVC) nhưng các daemon bị tắt. Cần 2 bước: start lại cluster + start tunnel.

## Nhanh nhất (1 lệnh)

```bash
./infra/scripts/daily-restart.sh
```

Script tự:
- Start minikube nếu chưa chạy
- Đợi node + pods ready
- Re-patch ingress-nginx LoadBalancer (nếu bị reset)
- Re-patch ingress routes (`api-gateway` → `ecom-stack-api-gateway`, fix bug Helm alias)
- Kiểm tra hosts file
- In trạng thái cuối

Sau khi script xong, **mở terminal mới** chạy tunnel (xem bước 2).

## Các bước thủ công (nếu script lỗi)

### 1. Start minikube

```bash
minikube -p ecom status   # xem có đang chạy không
minikube -p ecom start    # nếu chưa
```

### 2. Start tunnel (terminal RIÊNG, giữ mở)

```bash
minikube -p ecom tunnel
```

> ⚠️ Terminal này **block vĩnh viễn**. Ctrl+C mới tắt. Nếu tắt tunnel, browser sẽ mất kết nối.

### 3. Truy cập

- Browser: **http://ecom.local/**
- K8s dashboard: terminal khác, `minikube -p ecom dashboard`

## Check nhanh sau khi start

```bash
# Pods
kubectl -n ecom get pods

# Services
kubectl -n ecom get svc

# Ingress routes
kubectl -n ecom get ingress

# Frontend response
curl -sI http://ecom.local/   # expect 200
```

## Edge cases

### Trường hợp A: Chỉ tunnel chết (không restart PC)

Không cần chạy `daily-restart.sh`. Chỉ cần:

```bash
minikube -p ecom tunnel
```

Cluster + pods vẫn chạy bình thường.

### Trường hợp B: Restart PC hoàn toàn

Chạy `daily-restart.sh` (script xử lý tất cả).

### Trường hợp C: Cluster bị xoá (`minikube delete` hoặc lỗi)

Cần setup lại từ đầu:

```bash
./infra/scripts/teardown.sh   # xoá namespace nếu còn
minikube -p ecom delete
./infra/scripts/all.sh        # chạy lại toàn bộ first-time
```

### Trường hợp D: Pods ImagePullBackOff sau khi update code

```bash
eval $(minikube -p ecom docker-env)
./infra/scripts/build-images.sh
kubectl -n ecom rollout restart deployment <service-name>
```

### Trường hợp E: 502 Bad Gateway ngay sau khi tunnel start

Đợi 5-10 giây rồi refresh browser. Tunnel cần thời gian apply route vào bảng routing của Windows.

## Commands tham khảo (không cần nhớ, tra ở đây)

| Mục đích | Lệnh |
|---|---|
| Xem pods | `kubectl -n ecom get pods` |
| Xem services | `kubectl -n ecom get svc` |
| Xem ingress | `kubectl -n ecom get ingress` |
| Log của 1 pod | `kubectl -n ecom logs <pod-name>` |
| Log realtime | `kubectl -n ecom logs -f <pod-name>` |
| Exec vào pod | `kubectl -n ecom exec -it <pod-name> -- sh` |
| Restart deployment | `kubectl -n ecom rollout restart deployment <name>` |
| Trạng thái cluster | `minikube -p ecom status` |
| IP cluster | `minikube -p ecom ip` |
| Mở dashboard | `minikube -p ecom dashboard` |

## Liên quan

- Setup lần đầu: [02-first-time-setup.md](02-first-time-setup.md)
- Trạng thái hệ thống: [04-system-overview.md](04-system-overview.md)
- Troubleshooting: [05-troubleshooting.md](05-troubleshooting.md)
