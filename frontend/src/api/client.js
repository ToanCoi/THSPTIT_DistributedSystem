import axios from 'axios'
import { useAuthStore } from '@/stores/auth'

const API_BASE_URL = ''

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Request interceptor to attach JWT token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor for error handling
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

export default apiClient

// Auth API
export const authApi = {
  login: (data) => apiClient.post('/Auth/login', data),
  register: (data) => apiClient.post('/Auth/register', data),
  getMe: () => apiClient.get('/Auth/me')
}

// Customer API
export const customerApi = {
  getAll: () => apiClient.get('/Business/api/customers'),
  getPaging: (filter) => apiClient.get('/Business/api/customers/paging', { params: filter }),
  getById: (id) => apiClient.get(`/Business/api/customers/${id}`),
  create: (data) => apiClient.post('/Business/api/customers', data),
  update: (id, data) => apiClient.put(`/Business/api/customers/${id}`, data),
  delete: (id) => apiClient.delete(`/Business/api/customers/${id}`)
}

// Product API
export const productApi = {
  getAll: () => apiClient.get('/Business/api/products'),
  getPaging: (filter) => apiClient.get('/Business/api/products/paging', { params: filter }),
  getById: (id) => apiClient.get(`/Business/api/products/${id}`),
  create: (data) => apiClient.post('/Business/api/products', data),
  update: (id, data) => apiClient.put(`/Business/api/products/${id}`, data),
  delete: (id) => apiClient.delete(`/Business/api/products/${id}`)
}

// Stock API
export const stockApi = {
  getAll: () => apiClient.get('/Business/api/stocks'),
  getPaging: (filter) => apiClient.get('/Business/api/stocks/paging', { params: filter }),
  getById: (id) => apiClient.get(`/Business/api/stocks/${id}`),
  create: (data) => apiClient.post('/Business/api/stocks', data),
  update: (id, data) => apiClient.put(`/Business/api/stocks/${id}`, data),
  delete: (id) => apiClient.delete(`/Business/api/stocks/${id}`)
}

// Inward API
export const inwardApi = {
  getAll: () => apiClient.get('/Business/api/inwards'),
  getPaging: (filter) => apiClient.get('/Business/api/inwards/paging', { params: filter }),
  getById: (id) => apiClient.get(`/Business/api/inwards/${id}`),
  create: (data) => apiClient.post('/Business/api/inwards', data),
  update: (id, data) => apiClient.put(`/Business/api/inwards/${id}`, data),
  delete: (id) => apiClient.delete(`/Business/api/inwards/${id}`)
}

// Outward API
export const outwardApi = {
  getAll: () => apiClient.get('/Business/api/outwards'),
  getPaging: (filter) => apiClient.get('/Business/api/outwards/paging', { params: filter }),
  getById: (id) => apiClient.get(`/Business/api/outwards/${id}`),
  create: (data) => apiClient.post('/Business/api/outwards', data),
  update: (id, data) => apiClient.put(`/Business/api/outwards/${id}`, data),
  delete: (id) => apiClient.delete(`/Business/api/outwards/${id}`)
}

// Order API
export const orderApi = {
  getAll: () => apiClient.get('/Order/api/orders'),
  getPaging: (filter) => apiClient.get('/Order/api/orders/paging', { params: filter }),
  getById: (id) => apiClient.get(`/Order/api/orders/${id}`),
  create: (data) => apiClient.post('/Order/api/orders', data),
  update: (id, data) => apiClient.put(`/Order/api/orders/${id}`, data),
  delete: (id) => apiClient.delete(`/Order/api/orders/${id}`)
}

// Product Prices API
export const productPricesApi = {
  getSellingPrice: (productId) => apiClient.get(`/Business/api/productprices/${productId}/selling-price`),
  getStock: (productId, stockId) => apiClient.get(`/Business/api/productprices/${productId}/stock/${stockId}`)
}
