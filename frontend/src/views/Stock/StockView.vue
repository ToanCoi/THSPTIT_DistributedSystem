<template>
  <div>
    <v-row class="mb-4">
      <v-col cols="12">
        <h2 class="text-h4">Quản lý Kho hàng</h2>
      </v-col>
    </v-row>

    <v-card>
      <v-card-title>
        <v-row>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="search"
              prepend-inner-icon="mdi-magnify"
              label="Tìm kiếm"
              variant="outlined"
              density="compact"
              hide-details
            ></v-text-field>
          </v-col>
          <v-col cols="12" sm="6" class="text-right">
            <v-btn color="primary" @click="refresh" :loading="stockStore.loading">
              <v-icon left>mdi-refresh</v-icon>
              Làm mới
            </v-btn>
          </v-col>
        </v-row>
      </v-card-title>

      <v-data-table
        :headers="headers"
        :items="stockStore.stocks"
        :search="search"
        :loading="stockStore.loading"
      >
      </v-data-table>
    </v-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useStockStore } from '@/stores/stock'

const stockStore = useStockStore()

const search = ref('')

const headers = [
  { title: 'Mã kho', key: 'stock_code' },
  { title: 'Tên kho', key: 'stock_name' },
  { title: 'Địa chỉ', key: 'address' }
]

const refresh = () => {
  stockStore.fetchAll()
}

onMounted(() => {
  stockStore.fetchAll()
})
</script>