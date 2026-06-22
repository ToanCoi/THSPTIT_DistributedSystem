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
            <v-btn color="primary" @click="openDialog()">
              <v-icon left>mdi-plus</v-icon>
              Thêm kho
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
        <template v-slot:item.actions="{ item }">
          <v-btn icon size="small" color="primary" variant="text" @click="openDialog(item)">
            <v-icon>mdi-pencil</v-icon>
          </v-btn>
          <v-btn icon size="small" color="error" variant="text" @click="confirmDelete(item)">
            <v-icon>mdi-delete</v-icon>
          </v-btn>
        </template>
      </v-data-table>
    </v-card>

    <!-- Add/Edit Dialog -->
    <v-dialog v-model="dialog" max-width="500">
      <v-card>
        <v-card-title>{{ isEdit ? 'Sửa kho' : 'Thêm kho mới' }}</v-card-title>
        <v-card-text>
          <v-form ref="form" @submit.prevent="saveStock">
            <v-text-field
              v-model="formData.stock_code"
              label="Mã kho"
              variant="outlined"
              required
            ></v-text-field>

            <v-text-field
              v-model="formData.stock_name"
              label="Tên kho"
              variant="outlined"
              required
            ></v-text-field>

            <v-text-field
              v-model="formData.address"
              label="Địa chỉ"
              variant="outlined"
              required
            ></v-text-field>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="dialog = false">Hủy</v-btn>
          <v-btn color="primary" @click="saveStock" :loading="saving">
            {{ isEdit ? 'Lưu' : 'Thêm' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation Dialog -->
    <v-dialog v-model="deleteDialog" max-width="400">
      <v-card>
        <v-card-title>Xác nhận xóa</v-card-title>
        <v-card-text>
          Bạn có chắc muốn xóa kho "{{ selectedStock?.stock_name }}" không?
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="deleteDialog = false">Hủy</v-btn>
          <v-btn color="error" @click="deleteStock" :loading="deleting">Xóa</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useStockStore } from '@/stores/stock'

const stockStore = useStockStore()

const search = ref('')
const dialog = ref(false)
const deleteDialog = ref(false)
const saving = ref(false)
const deleting = ref(false)
const form = ref(null)

const isEdit = computed(() => !!formData.value.stock_id)
const selectedStock = ref(null)

const headers = [
  { title: 'Mã kho', key: 'stock_code' },
  { title: 'Tên kho', key: 'stock_name' },
  { title: 'Địa chỉ', key: 'address' },
  { title: 'Thao tác', key: 'actions', sortable: false }
]

const formData = ref({
  stock_id: null,
  stock_code: '',
  stock_name: '',
  address: ''
})

const openDialog = (item = null) => {
  if (item) {
    formData.value = {
      stock_id: item.stock_id,
      stock_code: item.stock_code,
      stock_name: item.stock_name,
      address: item.address
    }
  } else {
    formData.value = {
      stock_id: null,
      stock_code: '',
      stock_name: '',
      address: ''
    }
  }
  dialog.value = true
}

const confirmDelete = (item) => {
  selectedStock.value = item
  deleteDialog.value = true
}

const saveStock = async () => {
  try {
    saving.value = true
    const { stock_id, ...data } = formData.value
    if (stock_id) {
      await stockStore.update(stock_id, data)
    } else {
      await stockStore.create(data)
    }
    dialog.value = false
  } catch (err) {
    console.error('Failed to save stock:', err)
  } finally {
    saving.value = false
  }
}

const deleteStock = async () => {
  try {
    deleting.value = true
    await stockStore.remove(selectedStock.value.stock_id)
    deleteDialog.value = false
  } catch (err) {
    console.error('Failed to delete stock:', err)
  } finally {
    deleting.value = false
  }
}

onMounted(() => {
  stockStore.fetchAll()
})
</script>
