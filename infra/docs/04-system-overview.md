# Hệ thống sau khi chạy

Tài liệu này mô tả trạng thái hệ thống khi `all.sh` chạy thành công.

## Kiến trúc

```
                    Internet
                       │
                       ▼
        ┌──────────────────────────────┐
        │  nginx-ingress controller    │  (NodePort 30080 → LoadBalancer qua tunnel)
        │  namespace: ingress-nginx    │
        └──────────┬───────────────────┘
                   │
        ┌──────────┼──────────┬──────────────┐
        ▼          ▼          ▼              ▼
     /         /Auth      /Business       /kibana
   frontend  ┌──────────────────────┐      Kibana
   (Vue)     │   api-gateway (.NET) │      (5601)
             └──┬────┬────┬─────────┘
                ▼    ▼    ▼
              auth  biz  order
              api   api   api
                │    │    │
                ▼    ▼    ▼
        ┌──────────────────────┐       ┌──────────┐
        │ MySQL×2 + Kafka      │ ◄─── │ 3 workers│
        │ mysqlmaster (master_db)│    │ handle   │
        │ mysqlbusiness(business_db) │  ledger   │
        │ kafka (KRaft, apache/)      │ voucher  │
        └──────────────────────┘       └──────────┘

  Log pipeline:
  Pod stdout → kubelet → /var/log/containers/*.log
       → Filebeat (DaemonSet, namespace: logging)
       → Logstash (port 5044)
       → Elasticsearch
       → Kibana (port 5601)
```

## Bảng 16 pods (namespace `ecom`)

| Service | Replicas | Image | Port | Mục đích |
|---|---|---|---|---|
| `ecom-stack-api-gateway` | 2 | `ecom/api-gateway:v1.0.0-local` | 80 | Gateway, route /Auth /Business /Order |
| `ecom-stack-auth-api` | 2 | `ecom/auth-api:v1.0.0-local` | 80 | Auth (login, register, JWT) |
| `ecom-stack-business-api` | 2 | `ecom/business-api:v1.0.0-local` | 80 | Customer, Product, Stock, Inward, Outward |
| `ecom-stack-order-api` | 2 | `ecom/order-api:v1.0.0-local` | 80 | Order CRUD |
| `ecom-stack-frontend` | 2 | `ecom/frontend:v1.0.0-local` | 80 | Vue SPA |
| `ecom-stack-handle-worker` | 1 | `ecom/handle-worker:v1.0.0-local` | – | Consume `order-created`, fan-out |
| `ecom-stack-ledger-worker` | 1 | `ecom/ledger-worker:v1.0.0-local` | – | Consume `ledger-change` |
| `ecom-stack-voucher-worker` | 1 | `ecom/voucher-worker:v1.0.0-local` | – | Consume `voucher-change` |
| `ecom-stack-mysqlmaster-0` | 1 | `mysql:8.0` (override bitnami) | 3306 | Database `master_db` |
| `ecom-stack-mysqlbusiness-0` | 1 | `docker.io/mysql:8.0` | 3306 | Database `business_db` |
| `kafka-0` | 1 | `apache/kafka:3.8.1` (KRaft) | 9092, 9093 | Message broker |

## Namespaces

| Namespace | Nội dung | Tạo bởi |
|---|---|---|
| `ecom` | Toàn bộ microservices + MySQL + Kafka | `deploy-stack.sh` + `kafka.yaml` |
| `logging` | Elasticsearch, Kibana, Logstash, Filebeat | `install-elk.sh` |
| `ingress-nginx` | Nginx ingress controller | `setup-minikube.sh` (addon) |
| `kubernetes-dashboard` | K8s web UI | `minikube dashboard` (auto) |
| `default`, `kube-system`, `kube-public` | Kubernetes built-in | Tự sinh khi cluster start |

## URLs truy cập

| Mục đích | URL | Ghi chú |
|---|---|---|
| Frontend (Vue SPA) | `http://ecom.local/` | Trang chính |
| API Auth | `http://ecom.local/Auth/...` | login, register, /me |
| API Business | `http://ecom.local/Business/...` | customers, products, stocks, inwards, outwards |
| API Order | `http://ecom.local/Order/...` | orders |
| Kibana | `http://ecom.local/kibana` | ELK dashboard |
| K8s dashboard | `minikube -p ecom dashboard` | Cluster UI (terminal riêng) |

> ℹ️ Tất cả URL truy cập qua `ecom.local` đều yêu cầu `minikube tunnel` đang chạy + hosts có `127.0.0.1 ecom.local`.

## Services (ClusterIP)

| Service | Cluster IP | Port | Mục đích |
|---|---|---|---|
| `ecom-stack-api-gateway` | (auto) | 80 | Main entry |
| `ecom-stack-auth-api` | (auto) | 80 | Auth internal |
| `ecom-stack-business-api` | (auto) | 80 | Business internal |
| `ecom-stack-order-api` | (auto) | 80 | Order internal |
| `ecom-stack-frontend` | (auto) | 80 | Frontend |
| `ecom-stack-mysqlmaster` | (auto) | 3306 | MySQL master_db |
| `ecom-stack-mysqlbusiness` | (auto) | 3306 | MySQL business_db |
| `kafka` | (auto) | 9092, 9093 | Kafka broker |
| `kafka-headless` | None | 9092, 9093 | StatefulSet headless |
| `ecom-stack-mysqlmaster-headless` | None | 3306 | StatefulSet headless |
| `ecom-stack-mysqlbusiness-headless` | None | 3306 | StatefulSet headless |

## Ingress (ecom namespace)

| Host | Path | Backend service |
|---|---|---|
| `ecom.local` | `/Auth` (Prefix) | `ecom-stack-api-gateway:80` |
| `ecom.local` | `/Business` (Prefix) | `ecom-stack-api-gateway:80` |
| `ecom.local` | `/Order` (Prefix) | `ecom-stack-api-gateway:80` |
| `ecom.local` | `/` (Prefix) | `ecom-stack-frontend:80` |
| `ecom.local` | `/kibana` (Prefix) | `kibana:5601` (logging namespace, thường fail) |

## Kafka topics (tự tạo khi chạy)

| Topic | Producer | Consumer |
|---|---|---|
| `order-created` | order-api | handle-worker |
| `ledger-change` | handle-worker | ledger-worker |
| `voucher-change` | handle-worker | voucher-worker |

## MySQL databases

| Database | Service | User | Password |
|---|---|---|---|
| `master_db` | `ecom-stack-mysqlmaster:3306` | `root` | `P@ssw0rd123` |
| `business_db` | `ecom-stack-mysqlbusiness:3306` | `root` | `P@ssw0rd123` |

## Tài nguyên (resource requests/limits)

Mỗi service có 2 replicas (trừ workers + stateful = 1). Tổng RAM yêu cầu ~6-7 GB khi đầy tải. Tổng CPU ~3-4 cores.

## Liên quan

- Setup: [02-first-time-setup.md](02-first-time-setup.md)
- Daily: [03-daily-restart.md](03-daily-restart.md)
- Troubleshooting: [05-troubleshooting.md](05-troubleshooting.md)
- Scripts catalog: [01-deploy-scripts.md](01-deploy-scripts.md)
