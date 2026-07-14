# Sơ đồ hệ thống (C4 Model)

Tài liệu này dùng mô hình **C4** để mô tả kiến trúc ở 3 mức: Context → Container → Deployment.

---

## Level 1 — System Context

Thể hiện hệ thống trong bối cảnh tổng thể: ai dùng, giao tiếp với hệ thống ngoài nào.

```
                            ┌─────────────────────────┐
                            │        Người dùng        │
                            │  (khách hàng / admin /   │
                            │    nhân viên kho)        │
                            └────────────┬─────────────┘
                                         │ HTTPS
                                         ▼
                            ┌─────────────────────────┐
                            │                         │
                            │   THSPTIT_Distributed   │
                            │        System           │
                            │                         │
                            │  Microservice bán hàng  │
                            │   B2C + quản lý kho     │
                            │                         │
                            └─────────────────────────┘
```

Trong phạm vi demo, **không có hệ thống ngoài** nào được tích hợp (không có payment gateway, không có email service, không có SSO thật). Google OAuth chỉ là stub.

---

## Level 2 — Container

Mỗi **container** là 1 process chạy độc lập (1 process = 1 service hoặc 1 worker).

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         THSPTIT_DistributedSystem                            │
│                                                                             │
│   ┌──────────────────┐                                                      │
│   │   Frontend SPA   │  Vue 3 + Vuetify                                    │
│   │   (nginx)        │  Port: 3000 (dev) / 80 (k8s)                       │
│   └────────┬─────────┘                                                      │
│            │ /Auth, /Business, /Order                                       │
│            ▼                                                                │
│   ┌──────────────────┐                                                      │
│   │   ApiGateway     │  ASP.NET Core + HttpClient                          │
│   │                  │  Port: 5000 (dev) / 80 (k8s)                       │
│   └──┬─────┬─────┬───┘                                                      │
│      │     │     │                                                          │
│      │     │     │  /Auth/* → AuthApi                                     │
│      │     │     │  /Business/* → BusinessApi                             │
│      │     │     │  /Order/* → OrderApi                                   │
│      │     │     │                                                          │
│   ┌──▼──┐ ┌▼─────────┐ ┌▼─────────┐                                       │
│   │Auth │ │ Business │ │  Order   │                                       │
│   │ API │ │   API    │ │   API    │                                       │
│   │5001 │ │  5002    │ │  5003    │                                       │
│   └──┬──┘ └────┬─────┘ └────┬─────┘                                       │
│      │         │             │                                              │
│      │         │             │ publish: order-created, ledger-change      │
│      │         │             │                                              │
│      │    ┌────▼─────────────▼─────┐                                        │
│      │    │       Kafka           │  Topics:                               │
│      │    │   (KRaft, 9092/9093)   │  - order-created                     │
│      │    └────┬─────────────┬─────┘  - ledger-change                     │
│      │         │             │                                              │
│      │         │             │ subscribe                                   │
│      │    ┌────▼─────┐  ┌────▼──────┐                                       │
│      │    │ Voucher  │  │  Ledger   │                                       │
│      │    │ Worker   │  │  Worker   │                                       │
│      │    │          │  │           │                                       │
│      │    └────┬─────┘  └─────┬─────┘                                      │
│      │         │              │                                              │
│      ▼         ▼              ▼                                              │
│   ┌──────────────────┐  ┌──────────────────┐                                │
│   │    MySQL         │  │    MySQL         │                                │
│   │   master_db      │  │  business_db     │                                │
│   │   (port 3306)    │  │  (port 3306)     │                                │
│   └──────────────────┘  └──────────────────┘                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Tổng cộng 11 container**:
- 1 frontend SPA
- 1 API Gateway
- 4 API service (Auth, Business, Order, [4th API trong k8s qua helm])
- 2 worker (Voucher, Ledger)
- 2 MySQL instance
- 1 Kafka

Lưu ý: trong k8s thực tế chỉ có 4 API (Auth/Business/Order/... không có 4th — Gateway không tính là API). Workers chạy dưới dạng Deployment 1 replica.

---

## Level 3 — Deployment

Hệ thống có **2 môi trường triển khai**:

### Dev local — Docker Compose

```
┌─────────────────────────────────────────────────────────┐
│                   Docker host                            │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │ Network: ecom-net (bridge)                        │   │
│  │                                                   │   │
│  │  ┌─────────────┐  ┌─────────────┐                │   │
│  │  │ api-gateway │  │  auth-api   │                │   │
│  │  │  :5000      │  │   :5001     │                │   │
│  │  └─────────────┘  └──────┬──────┘                │   │
│  │                          │                         │   │
│  │  ┌─────────────┐  ┌──────▼──────┐  ┌──────────┐  │   │
│  │  │ business-api│  │  order-api  │  │voucher-  │  │   │
│  │  │  :5002      │  │   :5003     │  │worker    │  │   │
│  │  └─────────────┘  └─────────────┘  └──────────┘  │   │
│  │                                                   │   │
│  │  ┌─────────────┐  ┌─────────────┐                │   │
│  │  │ledger-worker│  │   zookeeper │                │   │
│  │  └─────────────┘  └──────┬──────┘                │   │
│  │                          │                         │   │
│  │  ┌─────────────┐  ┌──────▼──────┐                │   │
│  │  │ kafka       │  │mysql-master │                │   │
│  │  │  :9092/9093 │  │    :3306    │                │   │
│  │  └─────────────┘  └─────────────┘                │   │
│  │                                                   │   │
│  │  ┌─────────────┐                                  │   │
│  │  │mysql-business│                                 │   │
│  │  │    :3307     │                                  │   │
│  │  └─────────────┘                                  │   │
│  └──────────────────────────────────────────────────┘   │
│                                                          │
│  Volumes: mysql_master_data, mysql_business_data         │
└─────────────────────────────────────────────────────────┘
```

**File**: `backend/docker-compose.yml`

### Production-like — Kubernetes trên minikube

```
┌─────────────────────────────────────────────────────────────┐
│                  minikube cluster (ecom)                     │
│                                                              │
│   ┌───────────────────────┐                                 │
│   │  ingress-nginx        │  (addon, LoadBalancer via      │
│   │  (namespace:          │   minikube tunnel → host:80)   │
│   │   ingress-nginx)      │                                 │
│   └──────────┬────────────┘                                 │
│              │ host: ecom.local                              │
│              ▼                                              │
│   ┌────────────────────────────────────────────┐             │
│   │  Namespace: ecom                            │             │
│   │                                             │             │
│   │  ┌──────────────┐  ┌──────────────┐         │             │
│   │  │  frontend    │  │ api-gateway  │         │             │
│   │  │  Deployment  │  │ Deployment   │         │             │
│   │  │  (2 replicas)│  │ (2 replicas) │         │             │
│   │  └──────────────┘  └──────┬───────┘         │             │
│   │                           │                  │             │
│   │  ┌──────────────┐  ┌──────▼──────┐          │             │
│   │  │ auth-api     │  │ business-api│          │             │
│   │  │ (2 replicas) │  │ (2 replicas)│          │             │
│   │  └──────┬───────┘  └──────┬──────┘          │             │
│   │         │                 │                  │             │
│   │  ┌──────▼───────┐  ┌──────▼──────┐          │             │
│   │  │ order-api    │  │ voucher-    │          │             │
│   │  │ (2 replicas) │  │ worker (1)  │          │             │
│   │  └──────────────┘  └─────────────┘          │             │
│   │                                              │             │
│   │  ┌──────────────┐  ┌──────────────┐         │             │
│   │  │ ledger-worker│  │   kafka      │         │             │
│   │  │   (1)        │  │ (StatefulSet)│         │             │
│   │  └──────────────┘  └──────────────┘         │             │
│   │                                              │             │
│   │  MySQL: CHẠY NGOÀI cluster                  │             │
│   │  (host.minikube.internal:3306,               │             │
│   │   đã có sẵn trên Windows host)               │             │
│   └──────────────────────────────────────────────┘             │
│                                                              │
│   ┌───────────────────────┐                                 │
│   │  Namespace: logging   │                                 │
│   │  Elasticsearch + Kibana + Logstash + Filebeat           │
│   └───────────────────────┘                                 │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

**MySQL ngoài cluster**: Từ khi Bitnami gỡ Docker Hub free tier (late 2025), MySQL Helm chart đã được tắt. Thay vào đó, MySQL chạy ngoài cluster trên Windows host, pod truy cập qua DNS đặc biệt `host.minikube.internal`. Helm values: `infra/charts/ecom-stack/values.yaml` → `global.localMysql`.

**Kafka trong cluster, dùng image `apache/kafka:3.8.1`**: cũng vì Bitnami bị gỡ. Deploy qua raw manifest `infra/k8s/kafka.yaml` (KHÔNG qua Helm umbrella). Mode KRaft (không cần Zookeeper).

Chi tiết về deployment: xem [../03-deployment/k8s-deploy.md](../03-deployment/k8s-deploy.md).

---

## So sánh 2 môi trường

| Tiêu chí | Docker Compose (dev) | Kubernetes (k8s) |
|---|---|---|
| **Mục đích** | Dev local, test nhanh | Demo môi trường phân tán, kiểm thử production-like |
| **Số service** | 9 (7 service + 2 worker) | 11 (8 service + 2 worker + frontend) |
| **Replica** | 1 mỗi service | 2 cho API/frontend, 1 cho worker/stateful |
| **Service discovery** | Docker DNS (ecom-net) | Kubernetes Service + DNS |
| **Ingress** | Không có (port mapping) | nginx-ingress addon + Helm chart |
| **Logging** | NLog file trong container | NLog file + ELK stack (Filebeat → Logstash → ES → Kibana) |
| **Secret/config** | docker-compose env | Kubernetes Secret + ConfigMap |
| **MySQL** | Container trong compose | Ngoài cluster (Windows host) |
| **Kafka** | Container trong compose (cp-kafka + zookeeper) | StatefulSet trong cluster (apache/kafka KRaft) |
| **Khởi động** | `docker compose up -d` (5 phút) | `./infra/scripts/all.sh` (15-20 phút) |
| **Tắt** | `docker compose down` | `minikube -p ecom stop` |