<template>
  <v-app>
    <v-app-bar color="primary" dark app>
      <v-app-bar-title>Hệ thống Phân tán - Microservice</v-app-bar-title>
      <v-spacer></v-spacer>
      <v-btn v-if="authStore.isAuthenticated" @click="logout" icon>
        <v-icon>mdi-logout</v-icon>
      </v-btn>
    </v-app-bar>

    <v-navigation-drawer v-if="authStore.isAuthenticated" app permanent rail>
      <v-list-item title="Menu" nav>
        <template v-slot:prepend>
          <v-icon>mdi-account</v-icon>
        </template>
      </v-list-item>
      <v-divider></v-divider>
      <v-list density="compact" nav>
        <v-list-item to="/customers" prepend-icon="mdi-account-group" title="Khách hàng"></v-list-item>
        <v-list-item to="/products" prepend-icon="mdi-package-variant" title="Sản phẩm"></v-list-item>
        <v-list-item to="/stocks" prepend-icon="mdi-warehouse" title="Kho hàng"></v-list-item>
        <v-list-item to="/inwards" prepend-icon="mdi-arrow-down-bold" title="Nhập kho"></v-list-item>
        <v-list-item to="/outwards" prepend-icon="mdi-arrow-up-bold" title="Xuất kho"></v-list-item>
        <v-list-item to="/orders" prepend-icon="mdi-cart" title="Đơn hàng"></v-list-item>
      </v-list>
    </v-navigation-drawer>

    <v-main>
      <v-container fluid>
        <router-view />
      </v-container>
    </v-main>

    <v-footer app class="text-center d-flex justify-center">
      <span>&copy; 2024 Hệ thống Phân tán - Microservice</span>
    </v-footer>
  </v-app>
</template>

<script setup>
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'

const authStore = useAuthStore()
const router = useRouter()

const logout = () => {
  authStore.logout()
  router.push('/login')
}
</script>