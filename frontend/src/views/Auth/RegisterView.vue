<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card class="elevation-12">
          <v-card-title class="text-center bg-primary">
            <h3>Đăng ký</h3>
          </v-card-title>
          <v-card-text>
            <v-form @submit.prevent="handleRegister">
              <v-text-field
                v-model="formData.username"
                label="Tên đăng nhập"
                prepend-icon="mdi-account"
                variant="outlined"
                required
              ></v-text-field>
              <v-text-field
                v-model="formData.password"
                label="Mật khẩu"
                prepend-icon="mdi-lock"
                type="password"
                variant="outlined"
                required
              ></v-text-field>
              <v-text-field
                v-model="formData.email"
                label="Email"
                prepend-icon="mdi-email"
                type="email"
                variant="outlined"
                required
              ></v-text-field>
              <v-text-field
                v-model="formData.full_name"
                label="Họ và tên"
                prepend-icon="mdi-card-account-details"
                variant="outlined"
                required
              ></v-text-field>
              <v-alert v-if="error" type="error" class="mt-3">{{ error }}</v-alert>
              <v-alert v-if="success" type="success" class="mt-3">Đăng ký thành công!</v-alert>
              <v-btn
                type="submit"
                color="primary"
                block
                class="mt-4"
                :loading="loading"
              >
                Đăng ký
              </v-btn>
            </v-form>
          </v-card-text>
          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn variant="text" to="/login">Đã có tài khoản? Đăng nhập</v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const formData = ref({
  username: '',
  password: '',
  email: '',
  full_name: ''
})
const loading = ref(false)
const error = ref('')
const success = ref(false)

const handleRegister = async () => {
  try {
    loading.value = true
    error.value = ''
    await authStore.register(formData.value)
    success.value = true
    setTimeout(() => {
      router.push('/login')
    }, 1500)
  } catch (err) {
    error.value = err.response?.data?.message || 'Đăng ký thất bại'
  } finally {
    loading.value = false
  }
}
</script>