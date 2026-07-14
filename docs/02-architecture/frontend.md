# Kiến trúc Frontend

## Tech stack

| Layer | Công nghệ | Version |
|---|---|---|
| Framework | Vue | 3.4.21 |
| UI library | Vuetify | 3.5.11 |
| State management | Pinia | 2.1.7 |
| Routing | Vue Router | 4.3.0 |
| HTTP client | Axios | 1.6.8 |
| Build tool | Vite | 5.2.0 |
| CSS | Sass | 1.72.0 |
| Icon | @mdi/font | 7.4.47 |
| Testing | Vitest | 2.1.9 |

## Cấu trúc thư mục

```
frontend/
├── src/
│   ├── main.js                 # Khởi tạo app (Pinia + Vuetify + Router)
│   ├── App.vue                 # Root component (v-app-bar + nav drawer)
│   ├── api/
│   │   └── client.js           # Axios instance + 8 nhóm API
│   ├── stores/                 # Pinia stores (7 module)
│   │   ├── auth.js
│   │   ├── customer.js
│   │   ├── product.js
│   │   ├── stock.js
│   │   ├── inward.js
│   │   ├── outward.js
│   │   └── order.js
│   ├── views/                  # Page components (8 màn hình)
│   │   ├── Auth/
│   │   │   ├── LoginView.vue
│   │   │   └── RegisterView.vue
│   │   ├── Customer/CustomerView.vue
│   │   ├── Product/ProductView.vue
│   │   ├── Stock/StockView.vue
│   │   ├── Inward/InwardView.vue
│   │   ├── Outward/OutwardView.vue
│   │   └── Order/OrderView.vue
│   ├── router/
│   │   └── index.js            # 8 route + requiresAuth guard
│   ├── utils/
│   │   └── date.js             # Format date helper
│   ├── tests/                  # Vitest (chưa có test thực)
│   │   ├── setup.js
│   │   └── unit/
│   └── assets/                 # Static assets
├── public/                     # Public assets (index.html template nếu cần)
├── vite.config.js              # Vite config + dev proxy
├── package.json
├── Dockerfile                  # Multi-stage: node build → nginx runtime
├── nginx.conf                  # SPA fallback + gzip + cache
└── .dockerignore
```

## Routing

`frontend/src/router/index.js` — 8 route:

| Path | Component | Auth |
|---|---|---|
| `/login` | `LoginView` | Không |
| `/register` | `RegisterView` | Không |
| `/customers` | `CustomerView` | Có |
| `/products` | `ProductView` | Có |
| `/stocks` | `StockView` | Có |
| `/inwards` | `InwardView` | Có |
| `/outwards` | `OutwardView` | Có |
| `/orders` | `OrderView` | Có |
| `/` | redirect → `/login` | |
| `/:pathMatch(.*)*` | redirect → `/login` | |

**Guard** (`router.beforeEach`):
```js
if (to.meta.requiresAuth && !authStore.isAuthenticated) {
  next('/login')
} else if ((to.path === '/login' || to.path === '/register') && authStore.isAuthenticated) {
  next('/customers')
}
```

## State management — Pinia

### Pattern chung

Mỗi store có state `{ items, loading, error, pagination }` + actions `{ fetchAll, fetchPaging, create, update, remove }`.

Ví dụ `stores/order.js`:
```js
import { defineStore } from 'pinia'
import { orderApi } from '@/api/client'

export const useOrderStore = defineStore('order', {
  state: () => ({
    orders: [],
    loading: false,
    error: null,
    pagination: { skip: 0, take: 20, total: 0 }
  }),
  actions: {
    async fetchAll() {
      this.loading = true
      try {
        this.orders = await orderApi.getAll()
      } catch (err) {
        this.error = err.message
      } finally {
        this.loading = false
      }
    },
    async create(data) {
      const result = await orderApi.create(data)
      this.orders.push(result.data)
      return result.data
    },
    // ...
  }
})
```

### 7 stores

| Store | Dùng cho | API module |
|---|---|---|
| `auth.js` | Login, register, current user | `authApi` |
| `customer.js` | CRUD khách hàng | `customerApi` |
| `product.js` | CRUD sản phẩm | `productApi` |
| `stock.js` | CRUD kho | `stockApi` |
| `inward.js` | CRUD phiếu nhập | `inwardApi` |
| `outward.js` | CRUD phiếu xuất | `outwardApi` |
| `order.js` | CRUD đơn hàng | `orderApi` |

## API client — Axios

### Setup

`frontend/src/api/client.js`:
```js
import axios from 'axios'
import { useAuthStore } from '@/stores/auth'

const API_BASE_URL = ''   // Relative URL — proxy Vite hoặc ingress sẽ route

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' }
})

// Request interceptor — gắn JWT
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Response interceptor — auto logout khi 401
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      const authStore = useAuthStore()
      authStore.logout()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)
```

### 8 nhóm endpoint

| Nhóm | Endpoint pattern |
|---|---|
| `authApi` | `/Auth/login`, `/Auth/register`, `/Auth/me` |
| `customerApi` | `/Business/api/customers[/{id}]` + `/paging` |
| `productApi` | `/Business/api/products[/{id}]` + `/paging` |
| `stockApi` | `/Business/api/stocks[/{id}]` + `/paging` |
| `inwardApi` | `/Business/api/inwards[/{id}]` + `/paging` |
| `outwardApi` | `/Business/api/outwards[/{id}]` + `/paging` |
| `orderApi` | `/Order/api/orders[/{id}]` + `/paging` |
| `productPricesApi` | `/Business/api/productprices/{productId}/selling-price`, `/Business/api/productprices/{productId}/stock/{stockId}` |

Mỗi nhóm có method `getAll, getPaging, getById, create, update, delete` (trừ auth chỉ có `login, register, getMe`).

## Auth flow

```
1. User nhập username + password ở LoginView
2. authApi.login(credentials) → POST /Auth/login
3. AuthApi verify password (BCrypt), generate JWT + refresh_token
4. Response trả về { token, refresh_token, user: {...} }
5. Frontend lưu vào localStorage:
   - localStorage.token = JWT
   - localStorage.refreshToken = refresh_token
   - localStorage.user = user JSON
6. authStore.isAuthenticated = true
7. Router guard cho phép navigate vào các route requiresAuth
8. Axios interceptor tự gắn Bearer token vào mọi request
9. Khi token hết hạn (24h) → response 401 → axios interceptor logout + redirect /login
10. User login lại → token mới
```

## Vite dev server

`vite.config.js`:
```js
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) }
  },
  server: {
    port: 3000,
    proxy: {
      '/Auth': 'http://localhost:62739',     // ← port ASP.NET mặc định
      '/Business': 'http://localhost:62739',
      '/Order': 'http://localhost:62739'
    }
  }
})
```

**Lưu ý quan trọng**: port `62739` là port mặc định của ASP.NET khi không set `ASPNETCORE_URLS`. Nếu muốn dev với ApiGateway (port 5000), sửa proxy thành:
```js
proxy: {
  '/Auth': 'http://localhost:5000',
  '/Business': 'http://localhost:5000',
  '/Order': 'http://localhost:5000'
}
```

## Build cho production

### Local

```bash
cd frontend
npm install
npm run build
# Output: dist/ (static files)
```

### Docker

Multi-stage build (`frontend/Dockerfile`):
- **Stage 1**: `node:20-alpine` — cài dependencies, build ra `dist/`
- **Stage 2**: `nginx:1.27-alpine` — copy `dist/` + `nginx.conf`, EXPOSE 80

Build:
```bash
docker build -t ecom/frontend:v1.0.0-local -f frontend/Dockerfile frontend/
```

## nginx.conf (production)

`frontend/nginx.conf`:
```nginx
server {
  listen 80;
  server_name _;

  root /usr/share/nginx/html;
  index index.html;

  # Gzip
  gzip on;
  gzip_types text/plain text/css application/javascript application/json;

  # SPA fallback — mọi route không match file → index.html
  location / {
    try_files $uri $uri/ /index.html;
  }

  # Cache assets 1 năm
  location /assets/ {
    expires 1y;
    add_header Cache-Control "public, immutable";
  }

  # Health check
  location /health {
    return 200 "ok\n";
  }
}
```

## UI Components (Vuetify)

App.vue dùng các component Vuetify:
- `v-app` — root
- `v-app-bar` — header
- `v-navigation-drawer` — sidebar menu (6 mục: Khách hàng, Sản phẩm, Kho, Nhập kho, Xuất kho, Đơn hàng)
- `v-main` — content area
- `v-data-table` — bảng dữ liệu
- `v-dialog` — modal form create/edit
- `v-form` + `v-text-field`, `v-select` — form input
- `v-snackbar` — thông báo

Mỗi `*View.vue` có pattern chung:
1. Data table hiển thị list
2. Nút "Tạo mới" mở dialog form
3. Mỗi row có nút Edit + Delete
4. Loading state, error state hiển thị qua Vuetify components

## Out of scope (chưa làm)

- ❌ Unit test / E2E test (chỉ có setup file, chưa có test thực)
- ❌ i18n (đa ngôn ngữ)
- ❌ Dark mode
- ❌ Responsive mobile (chỉ test desktop)
- ❌ Pagination component (đang gọi API phân trang nhưng UI chưa hiển thị đẹp)
- ❌ Search/filter nâng cao
- ❌ Real-time update (WebSocket)