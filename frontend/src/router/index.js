import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

// Auth views
import LoginView from '@/views/Auth/LoginView.vue'
import RegisterView from '@/views/Auth/RegisterView.vue'

// Business views
import CustomerView from '@/views/Customer/CustomerView.vue'
import ProductView from '@/views/Product/ProductView.vue'
import StockView from '@/views/Stock/StockView.vue'
import InwardView from '@/views/Inward/InwardView.vue'
import OutwardView from '@/views/Outward/OutwardView.vue'
import OrderView from '@/views/Order/OrderView.vue'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: LoginView,
    meta: { requiresAuth: false }
  },
  {
    path: '/register',
    name: 'Register',
    component: RegisterView,
    meta: { requiresAuth: false }
  },
  {
    path: '/customers',
    name: 'Customers',
    component: CustomerView,
    meta: { requiresAuth: true }
  },
  {
    path: '/products',
    name: 'Products',
    component: ProductView,
    meta: { requiresAuth: true }
  },
  {
    path: '/stocks',
    name: 'Stocks',
    component: StockView,
    meta: { requiresAuth: true }
  },
  {
    path: '/inwards',
    name: 'Inwards',
    component: InwardView,
    meta: { requiresAuth: true }
  },
  {
    path: '/outwards',
    name: 'Outwards',
    component: OutwardView,
    meta: { requiresAuth: true }
  },
  {
    path: '/orders',
    name: 'Orders',
    component: OrderView,
    meta: { requiresAuth: true }
  },
  {
    path: '/',
    redirect: '/login'
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/login'
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Navigation guard
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next('/login')
  } else if ((to.path === '/login' || to.path === '/register') && authStore.isAuthenticated) {
    next('/customers')
  } else {
    next()
  }
})

export default router