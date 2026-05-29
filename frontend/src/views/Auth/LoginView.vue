<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card class="elevation-12">
          <v-card-title class="text-center bg-primary">
            <h3>Đăng nhập</h3>
          </v-card-title>
          <v-card-text>
            <v-form @submit.prevent="handleLogin">
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
                :type="showPassword ? 'text' : 'password'"
                variant="outlined"
                required
              >
                <template v-slot:append>
                  <v-icon @click="showPassword = !showPassword">
                    {{ showPassword ? 'mdi-eye-off' : 'mdi-eye' }}
                  </v-icon>
                </template>
              </v-text-field>
              <v-alert v-if="error" type="error" class="mt-3">{{ error }}</v-alert>
              <v-btn
                type="submit"
                color="primary"
                block
                class="mt-4"
                :loading="loading"
              >
                Đăng nhập
              </v-btn>
            </v-form>
          </v-card-text>
          <v-card-actions>
            <v-spacer></v-spacer>
            <v-btn variant="text" to="/register">Chưa có tài khoản? Đăng ký</v-btn>
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
  password: ''
})
const showPassword = ref(false)
const loading = ref(false)
const error = ref('')

const handleLogin = async () => {
  try {
    loading.value = true
    error.value = ''
    await authStore.login(formData.value.username, formData.value.password)
    router.push('/customers')
  } catch (err) {
    error.value = err.response?.data?.message || 'Đăng nhập thất bại'
  } finally {
    loading.value = false
  }
}
</script>