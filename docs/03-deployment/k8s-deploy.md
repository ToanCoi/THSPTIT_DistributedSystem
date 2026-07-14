# Triển khai Kubernetes local trên minikube

Tài liệu này hướng dẫn **build và triển khai** hệ thống lên Kubernetes local (minikube) bằng các script có sẵn trong `infra/scripts/`.

## 1. Quick start (1 lệnh)

Nếu máy đã có Docker Desktop + minikube + helm + kubectl + jq (xem [requirements.md](requirements.md)):

```bash
./infra/scripts/all.sh
```

Script sẽ chạy tuần tự:
1. `setup-minikube.sh` — tạo cluster `ecom`
2. `build-images.sh` — build 8 Docker image
3. `install-nginx-ingress.sh` — cài nginx ingress (nếu chưa có)
4. `install-elk.sh` — cài ELK stack
5. `deploy-stack.sh` — deploy umbrella chart
6. `seed-mysql.sh` — hỏi có seed data không

**Thời gian**: ~15-20 phút (build images chiếm phần lớn).

Sau khi xong:

```bash
# Terminal MỚI (giữ nguyên, không Ctrl+C):
minikube -p ecom tunnel
```

```powershell
# PowerShell Admin (chỉ làm 1 lần):
Add-Content C:\Windows\System32\drivers\etc\hosts "127.0.0.1 ecom.local"
ipconfig /flushdns
```

Mở browser: **http://ecom.local/**

---

## 2. Yêu cầu chi tiết

### 2.1 Công cụ

Xem [requirements.md § 2](requirements.md#2-công-cụ-theo-kịch-bản). Tóm tắt:

- Docker Desktop
- minikube
- kubectl
- Helm 3
- jq
- Git Bash (Windows)

### 2.2 MySQL ngoài cluster

Project **không chạy MySQL trong cluster** (vì Bitnami đã gỡ image khỏi Docker Hub free tier late 2025). Thay vào đó, MySQL chạy trên **Windows host**, pod truy cập qua DNS đặc biệt `host.minikube.internal`.

**Cài MySQL Server trên Windows**:
1. Download [MySQL Community Server 8.0](https://dev.mysql.com/downloads/mysql/)
2. Cài với password root = `Mysql!110720` (đúng với Helm values `global.localMysql.password`)
3. Chạy `init.sql` để tạo schema:
   ```bash
   mysql -h localhost -u root -pMysql!110720 < backend/Scripts/init.sql
   mysql -h localhost -u root -pMysql!110720 master_db < backend/Scripts/init.sql
   ```

**Hoặc dùng Docker container**:

```bash
docker run -d --name mysql-master \
  -e MYSQL_ROOT_PASSWORD=Mysql!110720 \
  -e MYSQL_DATABASE=master_db \
  -p 3306:3306 \
  -v ${PWD}/backend/Scripts:/docker-entrypoint-initdb.d \
  mysql:8.0
```

Lưu ý: container này chạy trên Docker Desktop (không phải trong minikube) → pod trong minikube truy cập được qua `host.minikube.internal:3306`.

---

## 3. Script catalog (9 scripts trong `infra/scripts/`)

| Script | Mục đích | Khi nào dùng |
|---|---|---|
| `all.sh` | Chạy tất cả first-time theo thứ tự | Lần đầu setup máy mới |
| `setup-minikube.sh` | Tạo cluster `ecom` + addon ingress/metrics-server/storage-provisioner | Lần đầu, hoặc sau `minikube delete` |
| `build-images.sh` | Build 7 image backend + 1 frontend vào minikube daemon | Sau khi đổi code, hoặc lần đầu |
| `install-nginx-ingress.sh` | Cài nginx ingress thủ công qua Helm (legacy) | Thường không cần — addon đã có |
| `install-elk.sh` | Cài ELK vào namespace `logging` | Sau `setup-minikube` |
| `deploy-stack.sh` | Helm install umbrella `ecom-stack` + Kafka custom manifest | Sau `build-images` |
| `seed-mysql.sh` | Import `init.sql` vào MySQL | Sau `deploy-stack` (khi MySQL ready) |
| `daily-restart.sh` | Khởi động lại sau restart PC | Mỗi lần restart PC |
| `teardown.sh` | Xoá Helm release + namespace `ecom` + `logging` | Khi muốn dọn dẹp |

### Sơ đồ phụ thuộc

```
all.sh
 ├─→ setup-minikube.sh          (tạo cluster)
 ├─→ build-images.sh            (cần docker-env từ setup)
 ├─→ install-nginx-ingress.sh   (legacy, thường skip)
 ├─→ install-elk.sh             (cài logging stack)
 ├─→ deploy-stack.sh            (helm install)
 └─→ seed-mysql.sh              (optional, hỏi user)

daily-restart.sh                (chạy SAU khi cluster đã có)
teardown.sh                     (chạy khi muốn reset)
```

---

## 4. Setup lần đầu — từng bước

Nếu không dùng `all.sh` (hoặc nó bị lỗi giữa chừng), chạy từng script:

### 4.1 Setup minikube

```bash
./infra/scripts/setup-minikube.sh
```

Script này:
- Tạo cluster `ecom` với driver=docker, 4 CPU/10GB RAM/30GB disk
- Bật addon: ingress, metrics-server, storage-provisioner
- Set `DOCKER_HOST` trỏ vào Docker daemon của minikube (`eval $(minikube docker-env -p ecom)`)

Verify:
```bash
minikube -p ecom status
kubectl get nodes
# → ecom   Ready   control-plane   ...
```

### 4.2 Build images

```bash
./infra/scripts/build-images.sh
```

Build 7 image backend + 1 frontend, tag `v1.0.0-local`, đẩy vào minikube daemon.

Verify:
```bash
docker images | grep ecom/
# → ecom/api-gateway   v1.0.0-local
# → ecom/auth-api      v1.0.0-local
# → ecom/business-api  v1.0.0-local
# → ecom/order-api     v1.0.0-local
# → ecom/ledger-worker v1.0.0-local
# → ecom/voucher-worker v1.0.0-local
# → ecom/frontend      v1.0.0-local
```

### 4.3 Cài nginx ingress

Thường **không cần** vì addon ingress đã có từ `setup-minikube`. Nếu thiếu:

```bash
./infra/scripts/install-nginx-ingress.sh
```

Verify:
```bash
kubectl -n ingress-nginx get pods
# → ingress-nginx-controller-xxx   Running
```

### 4.4 Cài ELK

```bash
./infra/scripts/install-elk.sh
```

Cài Elasticsearch + Kibana + Logstash + Filebeat vào namespace `logging`.

**Lưu ý**: nếu máy < 8GB RAM, Elasticsearch có thể bị OOMKilled. Fix bằng cách giảm heap trong `infra/logging/elasticsearch-values.yaml`:
```yaml
esJavaOpts: "-Xms256m -Xmx256m"
```

Sau đó chạy lại:
```bash
helm upgrade --install elasticsearch elastic/elasticsearch \
  -n logging -f infra/logging/elasticsearch-values.yaml
```

Verify:
```bash
kubectl -n logging get pods
# → elasticsearch-0   Running
# → kibana-xxx        Running
# → logstash-xxx      Running
# → filebeat-xxx      Running
```

### 4.5 Deploy ecom-stack

```bash
./infra/scripts/deploy-stack.sh
```

Script này:
- Chạy `helm dependency update` cho umbrella chart `ecom-stack`
- `helm upgrade --install ecom-stack infra/charts/ecom-stack --set global.imageTag=v1.0.0-local --wait --timeout 8m`
- Apply `infra/k8s/kafka.yaml` (Kafka custom, vì Bitnami bị gỡ)

Verify:
```bash
kubectl -n ecom get pods
# ~11 pods Running

kubectl -n ecom get ingress
# → ecom   ...   ecom.local
```

### 4.6 Seed MySQL

```bash
./infra/scripts/seed-mysql.sh
```

Import `backend/Scripts/init.sql` vào MySQL trên Windows host. Script sẽ exec mysql client trong container MySQL trên host Docker Desktop.

Verify:
```bash
mysql -h host.minikube.internal -u root -pMysql!110720 -e "SHOW DATABASES"
# → information_schema
# → master_db
# → business_db

mysql -h host.minikube.internal -u root -pMysql!110720 business_db -e "SELECT * FROM customers"
# → Có 1 row mẫu
```

---

## 5. Truy cập ứng dụng

### 5.1 URL

| Mục đích | URL | Ghi chú |
|---|---|---|
| Frontend (Vue SPA) | `http://ecom.local/` | Trang chính |
| Login | `http://ecom.local/login` | |
| API Auth | `http://ecom.local/Auth/...` | login, register, /me |
| API Business | `http://ecom.local/Business/...` | customers, products, stocks, inwards, outwards |
| API Order | `http://ecom.local/Order/...` | orders |
| Kibana (ELK) | `http://ecom.local/kibana` | Thường fail vì ingress cross-namespace — xem §10.3 |
| K8s dashboard | `minikube -p ecom dashboard` | Cluster UI (terminal riêng) |

Yêu cầu: `minikube tunnel` đang chạy + hosts có `127.0.0.1 ecom.local`.

### 5.2 Login mặc định

- Username: `admin`
- Password: `admin123`

### 5.3 Verify nhanh

```bash
# 11 pods đều Running
kubectl -n ecom get pods

# Frontend response
curl -sI http://ecom.local/   # expect HTTP 200

# API gateway health
curl -sI http://ecom.local/health   # expect HTTP 200
```

---

## 6. Sau mỗi lần restart máy

Sau khi restart PC, minikube cluster vẫn còn (data persistent trong PVC) nhưng các daemon bị tắt.

### 6.1 Quick

```bash
./infra/scripts/daily-restart.sh
```

Script tự:
- Start minikube nếu chưa chạy
- Đợi node + pods ready
- Re-patch ingress-nginx LoadBalancer (nếu bị reset)
- Re-patch ingress routes (`api-gateway` → `ecom-stack-api-gateway`)
- Kiểm tra hosts file
- In trạng thái cuối

Sau khi script xong, **mở terminal mới** chạy tunnel.

### 6.2 Manual

```bash
# Start minikube
minikube -p ecom status
minikube -p ecom start

# Tunnel (terminal RIÊNG, giữ mở)
minikube -p ecom tunnel
```

---

## 7. Đổi code → rebuild image

Sau khi sửa code C# hoặc Vue:

```bash
# 1. Set docker-env (chỉ cần 1 lần sau khi mở Git Bash mới)
eval $(minikube -p ecom docker-env)

# 2. Build lại image (chỉ image đã đổi, hoặc build tất cả)
./infra/scripts/build-images.sh

# 3. Rollout restart deployment
kubectl -n ecom rollout restart deployment ecom-stack-business-api
kubectl -n ecom rollout restart deployment ecom-stack-frontend
```

Nếu đổi **Helm chart values** (không phải code):

```bash
helm upgrade --install ecom-stack infra/charts/ecom-stack \
  --set global.imageTag=v1.0.0-local
```

---

## 8. Kiến trúc k8s sau khi deploy

### 8.1 Namespaces

| Namespace | Nội dung | Tạo bởi |
|---|---|---|
| `ecom` | Toàn bộ microservices + Kafka | `deploy-stack.sh` + `kafka.yaml` |
| `logging` | Elasticsearch, Kibana, Logstash, Filebeat | `install-elk.sh` |
| `ingress-nginx` | Nginx ingress controller | `setup-minikube.sh` (addon) |
| `kubernetes-dashboard` | K8s web UI | `minikube dashboard` (auto) |
| `default`, `kube-system`, `kube-public` | Kubernetes built-in | Auto |

### 8.2 Services trong namespace `ecom`

| Service | Cluster IP | Port | Mục đích |
|---|---|---|---|
| `ecom-stack-api-gateway` | (auto) | 80 | Main entry |
| `ecom-stack-auth-api` | (auto) | 80 | Auth internal |
| `ecom-stack-business-api` | (auto) | 80 | Business internal |
| `ecom-stack-order-api` | (auto) | 80 | Order internal |
| `ecom-stack-frontend` | (auto) | 80 | Frontend |
| `kafka` | (auto) | 9092, 9093 | Kafka broker |
| `kafka-headless` | None | 9092, 9093 | StatefulSet headless |

MySQL **không có trong cluster** — chạy ngoài cluster (host.minikube.internal:3306).

### 8.3 Ingress routes

| Host | Path | Backend service |
|---|---|---|
| `ecom.local` | `/Auth` (Prefix) | `ecom-stack-api-gateway:80` |
| `ecom.local` | `/Business` (Prefix) | `ecom-stack-api-gateway:80` |
| `ecom.local` | `/Order` (Prefix) | `ecom-stack-api-gateway:80` |
| `ecom.local` | `/` (Prefix) | `ecom-stack-frontend:80` |

### 8.4 Helm chart structure

```
infra/charts/
├── api-gateway/                # Subchart local
├── auth-api/                   # Subchart local
├── business-api/               # Subchart local
├── order-api/                  # Subchart local
├── ledger-worker/              # Subchart local
├── voucher-worker/             # Subchart local
├── frontend/                   # Subchart local
└── ecom-stack/                 # Umbrella chart
    ├── Chart.yaml              # dependencies: 7 subchart local + mysql + kafka
    ├── values.yaml             # global config
    ├── charts/                 # .tgz của deps (đã build sẵn)
    └── templates/              # ingress, secrets, configmap, NOTES.txt
```

**Sửa subchart**: edit file trong `infra/charts/{subchart}/`, sau đó rebuild + redeploy:
```bash
cd infra/charts/ecom-stack
# Xóa .tgz cũ của subchart vừa sửa
rm charts/{subchart}-*.tgz
helm dependency update   # rebuild .tgz từ source

# Deploy lại
cd ../../..
./infra/scripts/deploy-stack.sh
```

Xem thêm quirk về Helm umbrella cache: [memory feedback_helm_dependency_tgz_cache.md](../../MEMORY.md).

---

## 9. Tài nguyên & monitoring

### 9.1 Resource requests/limits

Mỗi service có 2 replicas (trừ workers + stateful = 1). Tổng RAM yêu cầu ~6-7 GB khi đầy tải. Tổng CPU ~3-4 cores.

### 9.2 Xem log qua Kibana

```bash
# Mở Kibana (terminal khác)
kubectl -n logging port-forward svc/kibana 5601:5601
# Mở http://localhost:5601
```

Nếu không vào được Kibana qua ingress `/kibana`, dùng port-forward như trên.

### 9.3 Xem log trực tiếp

```bash
# Log 1 pod
kubectl -n ecom logs <pod-name>

# Log realtime
kubectl -n ecom logs -f <pod-name>

# Log tất cả pod có label
kubectl -n ecom logs -l app.kubernetes.io/name=business-api --tail 50
```

---

## 10. Troubleshooting

### 10.1 ImagePullBackOff / ErrImagePull

**Triệu chứng**: pod ở trạng thái `ImagePullBackOff`.

**Nguyên nhân**: image chưa được build hoặc `docker-env` chưa trỏ vào minikube daemon.

**Fix**:
```bash
eval $(minikube -p ecom docker-env)
docker images | grep ecom/
# Nếu không có image nào, chạy lại:
./infra/scripts/build-images.sh
```

### 10.2 Pod CrashLoopBackOff

**Fix**:
```bash
kubectl -n ecom logs <pod-name> --previous
kubectl -n ecom describe pod <pod-name>
```

Lỗi thường gặp:

| Lỗi | Nguyên nhân | Fix |
|---|---|---|
| `Connection refused` tới MySQL | MySQL trên host chưa ready | `mysql -h host.minikube.internal -u root -p` để verify |
| `Connection refused` tới Kafka | Kafka pod chưa ready | `kubectl -n ecom get pods -l app=kafka` |
| `Name or service not known` | Bug Helm alias | Sửa `infra/charts/api-gateway/values.yaml` thêm prefix `ecom-stack-`, rebuild |
| `Invalid JWT secret` | Secret `jwt-secret-key` chưa tạo | Chạy lại `deploy-stack.sh` |

### 10.3 502 Bad Gateway từ ingress

**Triệu chứng**: truy cập `http://ecom.local/...` trả về 502.

**Nguyên nhân**: ingress route trỏ sai service (vd: `api-gateway` thay vì `ecom-stack-api-gateway`).

**Fix**:
```bash
# Chạy daily-restart.sh để script tự patch
./infra/scripts/daily-restart.sh

# Hoặc patch tay
kubectl -n ecom get ingress ecom -o yaml > /tmp/ingress.yaml
# Sửa: api-gateway → ecom-stack-api-gateway, frontend → ecom-stack-frontend
kubectl -n ecom apply -f /tmp/ingress.yaml
```

### 10.4 503 Service Temporarily Unavailable

**Triệu chứng**: trả 503 ngay sau khi tunnel start.

**Fix**:
```bash
# Patch ingress service → LoadBalancer
kubectl -n ingress-nginx patch svc ingress-nginx-controller -p '{"spec":{"type":"LoadBalancer"}}'

# Đảm bảo tunnel đang chạy
minikube -p ecom tunnel
# Đợi 5-10s rồi refresh browser
```

### 10.5 Elasticsearch OOMKilled

**Triệu chứng**: pod elasticsearch bị kill vì OOM.

**Fix** (giảm heap):
```yaml
# infra/logging/elasticsearch-values.yaml
esJavaOpts: "-Xms256m -Xmx256m"
```

Hoặc tăng memory minikube:
```bash
minikube -p ecom stop
minikube -p ecom start --memory=12g
```

### 10.6 Kibana trống, không thấy log

**Fix**:
1. Kiểm tra Filebeat:
   ```bash
   kubectl -n logging get pods -l app=filebeat
   kubectl -n logging logs -l app=filebeat --tail=50
   ```
2. Kiểm tra Logstash:
   ```bash
   kubectl -n logging logs -l app=logstash --tail=50
   ```
3. Query trực tiếp Elasticsearch:
   ```bash
   kubectl -n logging exec elasticsearch-0 -- \
     curl -s "http://localhost:9200/_cat/indices?v"
   # Expect: có index ecom-logs-YYYY.MM.DD
   ```

### 10.7 Kafka consumer không nhận message

**Fix**:
1. Kiểm tra topic đã tạo chưa:
   ```bash
   MSYS_NO_PATHCONV=1 kubectl -n ecom exec kafka-0 -- \
     /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 --list
   ```
   Nếu rỗng → tạo topic:
   ```bash
   MSYS_NO_PATHCONV=1 kubectl -n ecom exec kafka-0 -- \
     /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 \
     --create --topic order-created --partitions 1 --replication-factor 1
   MSYS_NO_PATHCONV=1 kubectl -n ecom exec kafka-0 -- \
     /opt/kafka/bin/kafka-topics.sh --bootstrap-server localhost:9092 \
     --create --topic ledger-change --partitions 1 --replication-factor 1
   ```
2. Trigger 1 order qua API, xem log worker:
   ```bash
   kubectl -n ecom logs -l app.kubernetes.io/name=voucher-worker --tail=20
   ```

### 10.8 StorageClass missing

**Triệu chứng**: PVC pending mãi không bind.

**Fix**:
```bash
minikube -p ecom addons enable storage-provisioner
kubectl get storageclass
# Expect: standard mặc định
```

### 10.9 Frontend trắng / Vue app lỗi

**Triệu chứng**: truy cập `/` thấy trang trắng hoặc 404 cho các route con.

**Fix**:
- Kiểm tra `nginx.conf` đã có `try_files $uri $uri/ /index.html;` chưa (có sẵn trong `frontend/nginx.conf`).
- Build Vue app có thành công không — kiểm tra log build:
  ```bash
  eval $(minikube -p ecom docker-env)
  docker build -t ecom/frontend:v1.0.0-local -f frontend/Dockerfile frontend/
  ```
- Kiểm tra pod frontend log:
  ```bash
  kubectl -n ecom logs -l app.kubernetes.io/name=frontend --tail=20
  ```

### 10.10 Lỗi Helm dependency

**Triệu chứng**: `helm dependency update` báo lỗi `unable to load schemas/charts`.

**Fix**:
```bash
helm repo update
cd infra/charts/ecom-stack
helm dependency update
```

Nếu sửa subchart local mà không apply → phải xóa `.tgz` cũ + `helm dependency build`:
```bash
cd infra/charts/ecom-stack
rm charts/{subchart-name}-*.tgz
helm dependency build .
```

### 10.11 Git Bash + minikube quirks

Một số quirk hay gặp trên Windows:

1. **`minikube ssh -- "cmd $var"`** mở interactive SSH thay vì exec. Fix: dùng `eval $(minikube docker-env) && cmd`
2. **`for X in a b; do ...; done` sau eval** → syntax error. Fix: chạy từng lệnh riêng
3. **`kubectl exec ... /opt/...`** bị Git Bash convert thành Windows path. Fix: prepend `MSYS_NO_PATHCONV=1`
4. **`minikube` không tìm thấy trong PowerShell**: thêm vào PATH user-level:
   ```powershell
   [Environment]::SetEnvironmentVariable("Path", $env:Path + ";D:\Application\Devops", "User")
   ```

### 10.12 Xoá sạch và chạy lại

```bash
./infra/scripts/teardown.sh
minikube -p ecom delete
minikube -p ecom start --cpus=4 --memory=10g
./infra/scripts/setup-minikube.sh
./infra/scripts/build-images.sh
./infra/scripts/install-elk.sh
./infra/scripts/deploy-stack.sh
./infra/scripts/seed-mysql.sh
```