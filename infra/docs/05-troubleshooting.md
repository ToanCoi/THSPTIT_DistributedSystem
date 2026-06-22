# Troubleshooting

Lỗi thường gặp + cách fix. Nếu không tìm thấy ở đây, xem log chi tiết:

```bash
kubectl -n ecom logs <pod-name>
kubectl -n ecom describe pod <pod-name>
kubectl -n ecom get events --sort-by='.lastTimestamp'
```

---

## ImagePullBackOff / ErrImagePull

**Triệu chứng**: pod ở trạng thái `ImagePullBackOff` hoặc `ErrImagePull`.

**Nguyên nhân**: Image chưa được build hoặc `docker-env` chưa trỏ vào minikube daemon.

**Fix**:
```bash
eval $(minikube -p ecom docker-env)
docker images | grep ecom/
# Nếu không có image nào, chạy lại:
./infra/scripts/build-images.sh
```

**Lỗi bitnami/* (late 2025 trở đi)**: Bitnami đã gỡ toàn bộ image khỏi Docker Hub free tier. Nếu thấy `manifest for bitnami/mysql:8.0.33-... not found`, project đã có workaround:
- MySQL: dùng `docker.io/mysql:8.0` (override qua helm `--set mysqlmaster.image.repository=mysql --set mysqlmaster.image.tag=8.0`)
- Kafka: dùng custom `apache/kafka:3.8.1` (file `infra/k8s/kafka.yaml`)

---

## Pod CrashLoopBackOff

**Triệu chứng**: pod liên tục restart.

**Fix**:
```bash
kubectl -n ecom logs <pod-name> --previous
kubectl -n ecom describe pod <pod-name>
```

Các lỗi thường gặp:
- `Connection refused` tới MySQL: chờ MySQL pod ready (`kubectl -n ecom get pods -l app.kubernetes.io/name=mysqlmaster`).
- `Connection refused` tới Kafka: chờ Kafka pod ready (`kubectl -n ecom get pods -l app=kafka`).
- `Name or service not known` (api-gateway gọi auth-api): bug Helm alias, fix bằng cách sửa `infra/charts/api-gateway/values.yaml` thêm prefix `ecom-stack-` rồi rebuild.
- `Invalid JWT secret`: secret `jwt-secret-key` chưa có. Chạy lại `deploy-stack.sh`.

---

## 502 Bad Gateway từ ingress

**Triệu chứng**: truy cập `http://ecom.local/Auth/...` trả về 502.

**Nguyên nhân**: ingress route trỏ sai service (ví dụ `api-gateway` thay vì `ecom-stack-api-gateway`).

**Fix**: Chạy `daily-restart.sh` (script tự patch) HOẶC patch tay:
```bash
kubectl -n ecom get ingress ecom -o yaml > /tmp/ingress.yaml
# Sửa: api-gateway → ecom-stack-api-gateway, frontend → ecom-stack-frontend
kubectl -n ecom apply -f /tmp/ingress.yaml
```

---

## 503 Service Temporarily Unavailable

**Triệu chứng**: truy cập `http://ecom.local/...` trả 503.

**Nguyên nhân**: ingress-nginx-controller service chưa được patch thành LoadBalancer, hoặc tunnel chưa start.

**Fix**:
```bash
kubectl -n ingress-nginx patch svc ingress-nginx-controller -p '{"spec":{"type":"LoadBalancer"}}'
# Terminal riêng: minikube -p ecom tunnel
# Đợi 5-10s rồi refresh
```

---

## Elasticsearch OOMKilled

**Triệu chứng**: pod elasticsearch bị kill vì out-of-memory.

**Fix**: Trên máy < 8GB RAM, giảm heap trong `infra/logging/elasticsearch-values.yaml`:
```yaml
esJavaOpts: "-Xms256m -Xmx256m"
```

Hoặc tăng memory minikube:
```bash
minikube -p ecom stop
minikube -p ecom start --memory=12g
```

---

## Kibana trống, không thấy log

**Triệu chứng**: Kibana mở được nhưng tab Discover trống.

**Fix**:
1. Kiểm tra Filebeat có chạy không:
   ```bash
   kubectl -n logging get pods -l app=filebeat
   kubectl -n logging logs -l app=filebeat --tail=50
   ```
2. Kiểm tra Logstash parse được không:
   ```bash
   kubectl -n logging logs -l app=logstash --tail=50
   ```
3. Query trực tiếp Elasticsearch:
   ```bash
   kubectl -n logging exec elasticsearch-0 -- \
     curl -s "http://localhost:9200/_cat/indices?v"
   # Expect: có index ecom-logs-YYYY.MM.DD
   ```

---

## Nginx Ingress không route được

**Triệu chứng**: truy cập `http://ecom.local/` bị 404 hoặc 502.

**Fix**:
1. Kiểm tra ingress controller:
   ```bash
   kubectl -n ingress-nginx get pods
   ```
2. Kiểm tra ingress:
   ```bash
   kubectl -n ecom describe ingress ecom
   ```
3. Nếu dùng `minikube tunnel` thì truy cập `http://localhost`. Nếu dùng IP thì kiểm tra `/etc/hosts` đã có `ecom.local` chưa.

---

## Kafka consumer không nhận message

**Triệu chứng**: workers start nhưng không có log xử lý.

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
   ```
2. Trigger 1 order qua API, sau đó xem log worker:
   ```bash
   kubectl -n ecom logs -l app.kubernetes.io/name=voucher-worker --tail=20
   ```

---

## StorageClass missing

**Triệu chứng**: PVC pending mãi không bind.

**Fix**:
```bash
minikube -p ecom addons enable storage-provisioner
kubectl get storageclass
# Expect: standard mặc định
```

---

## Frontend trắng / Vue app lỗi

**Triệu chứng**: truy cập `/` thấy trang trắng hoặc 404 cho các route con.

**Fix**:
- Kiểm tra `nginx.conf` đã có `try_files $uri $uri/ /index.html;` chưa (đã có sẵn trong `frontend/nginx.conf`).
- Build Vue app có thành công không — kiểm tra log build:
  ```bash
  eval $(minikube -p ecom docker-env)
  docker build -t ecom/frontend:v1.0.0-local -f frontend/Dockerfile frontend/
  ```
- Kiểm tra pod frontend log:
  ```bash
  kubectl -n ecom logs -l app.kubernetes.io/name=frontend --tail=20
  ```
- Nếu API call fail với CORS hoặc `localhost:62739`: bug `API_BASE_URL` hardcode trong `frontend/src/api/client.js`. Fix: đổi thành `''` rồi rebuild.

---

## Xoá sạch và chạy lại

```bash
./infra/scripts/teardown.sh
minikube -p ecom delete
minikube -p ecom start --cpus=4 --memory=10g
./infra/scripts/setup-minikube.sh
./infra/scripts/build-images.sh
./infra/scripts/install-elk.sh
./infra/scripts/deploy-stack.sh
```

---

## Lỗi Helm dependency

**Triệu chứng**: `helm dependency update` báo lỗi `unable to load schemas/charts`.

**Fix**:
```bash
helm repo update
cd infra/charts/ecom-stack
helm dependency update
```

---

## Git Bash + minikube parsing quirks (Windows)

Các quirk hay gặp:

1. **`minikube ssh -- "cmd $var"`** mở interactive SSH thay vì exec. Fix: dùng `eval $(minikube docker-env) && cmd`
2. **`for X in a b; do ...; done` sau eval** → syntax error. Fix: chạy từng lệnh riêng
3. **`kubectl exec ... /opt/...`** bị Git Bash convert thành Windows path. Fix: prepend `MSYS_NO_PATHCONV=1`
4. **`minikube` không tìm thấy trong PowerShell**: thêm vào PATH user-level:
   ```powershell
   [Environment]::SetEnvironmentVariable("Path", $env:Path + ";D:\Application\Devops", "User")
   ```
