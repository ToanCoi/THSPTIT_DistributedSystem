<template>
  <div>
    <v-row class="mb-4">
      <v-col cols="12">
        <h2 class="text-h4">Quản lý Sản phẩm</h2>
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
              Thêm sản phẩm
            </v-btn>
          </v-col>
        </v-row>
      </v-card-title>

      <v-data-table
        :headers="headers"
        :items="productStore.products"
        :search="search"
        :loading="productStore.loading"
      >
        <template v-slot:item.price="{ item }">
          {{ formatCurrency(item.price) }}
        </template>
        <template v-slot:item.actions="{ item }">
          <v-icon size="small" class="mr-2" @click="openDialog(item)">mdi-pencil</v-icon>
          <v-icon size="small" @click="confirmDelete(item)">mdi-delete</v-icon>
        </template>
      </v-data-table>
    </v-card>

    <!-- Product Dialog -->
    <v-dialog v-model="dialog" max-width="500">
      <v-card>
        <v-card-title>{{ isEdit ? 'Sửa sản phẩm' : 'Thêm sản phẩm' }}</v-card-title>
        <v-card-text>
          <v-form ref="form" @submit.prevent="saveProduct">
            <v-text-field v-model="formData.product_code" label="Mã sản phẩm" variant="outlined" required :disabled="isEdit"></v-text-field>
            <v-text-field v-model="formData.product_name" label="Tên sản phẩm" variant="outlined" required></v-text-field>
            <v-text-field v-model="formData.price" label="Giá" variant="outlined" type="number" required></v-text-field>
            <v-text-field v-model="formData.unit" label="Đơn vị" variant="outlined"></v-text-field>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="dialog = false">Hủy</v-btn>
          <v-btn color="primary" @click="saveProduct" :loading="saving">{{ isEdit ? 'Lưu' : 'Thêm' }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation -->
    <v-dialog v-model="deleteDialog" max-width="400">
      <v-card>
        <v-card-title>Xác nhận xóa</v-card-title>
        <v-card-text>Bạn có chắc chắn muốn xóa sản phẩm này?</v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="deleteDialog = false">Hủy</v-btn>
          <v-btn color="error" @click="deleteProduct" :loading="deleting">Xóa</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useProductStore } from '@/stores/product'

const productStore = useProductStore()

const search = ref('')
const dialog = ref(false)
const deleteDialog = ref(false)
const isEdit = ref(false)
const saving = ref(false)
const deleting = ref(false)
const form = ref(null)
const selectedItem = ref(null)

const headers = [
  { title: 'Mã SP', key: 'product_code' },
  { title: 'Tên sản phẩm', key: 'product_name' },
  { title: 'Giá', key: 'price' },
  { title: 'Đơn vị', key: 'unit' },
  { title: 'Thao tác', key: 'actions', sortable: false }
]

const formData = ref({
  product_code: '',
  product_name: '',
  price: 0,
  unit: ''
})

const formatCurrency = (value) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)
}

onMounted(() => {
  productStore.fetchAll()
})

const openDialog = (item = null) => {
  if (item) {
    isEdit.value = true
    selectedItem.value = item
    formData.value = { ...item }
  } else {
    isEdit.value = false
    selectedItem.value = null
    formData.value = { product_code: '', product_name: '', price: 0, unit: '' }
  }
  dialog.value = true
}

const saveProduct = async () => {
  try {
    saving.value = true
    if (isEdit.value) {
      await productStore.update(selectedItem.value.product_id, formData.value)
    } else {
      await productStore.create(formData.value)
    }
    dialog.value = false
  } catch (err) {
    console.error('Failed to save product:', err)
  } finally {
    saving.value = false
  }
}

const confirmDelete = (item) => {
  selectedItem.value = item
  deleteDialog.value = true
}

const deleteProduct = async () => {
  try {
    deleting.value = true
    await productStore.remove(selectedItem.value.product_id)
    deleteDialog.value = false
  } catch (err) {
    console.error('Failed to delete product:', err)
  } finally {
    deleting.value = false
  }
}
</script>