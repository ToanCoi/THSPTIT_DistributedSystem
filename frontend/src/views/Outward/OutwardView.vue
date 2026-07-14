<template>
  <div>
    <v-row class="mb-4">
      <v-col cols="12">
        <h2 class="text-h4">Phiếu Xuất kho</h2>
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
              Tạo phiếu xuất
            </v-btn>
          </v-col>
        </v-row>
      </v-card-title>

      <v-data-table
        :headers="headers"
        :items="outwardStore.outwards"
        :search="search"
        :loading="outwardStore.loading"
      >
        <template v-slot:item.order_code="{ item }">
          <span v-if="item.order_code">{{ item.order_code }}</span>
          <span v-else class="text-grey">—</span>
        </template>
        <template v-slot:item.unit_price="{ item }">
          {{ formatCurrency(item.unit_price) }}
        </template>
        <template v-slot:item.total="{ item }">
          {{ formatCurrency(item.quantity * item.unit_price) }}
        </template>
        <template v-slot:item.outward_date="{ item }">
          {{ formatDate(item.outward_date) }}
        </template>
        <template v-slot:item.actions="{ item }">
          <v-tooltip
            v-if="item.order_id"
            text="Phiếu xuất gắn với đơn hàng - không thể sửa/xóa"
            location="top"
          >
            <template v-slot:activator="{ props }">
              <v-icon v-bind="props" size="small" class="mr-2 text-grey">mdi-pencil-off</v-icon>
              <v-icon v-bind="props" size="small" class="text-grey">mdi-delete-off</v-icon>
            </template>
          </v-tooltip>
          <template v-else>
            <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEditDialog(item)"></v-btn>
            <v-btn icon="mdi-delete" size="small" variant="text" @click="confirmDelete(item)"></v-btn>
          </template>
        </template>
      </v-data-table>
    </v-card>

    <!-- Outward Dialog -->
    <v-dialog v-model="dialog" max-width="500">
      <v-card>
        <v-card-title>{{ mode === 'create' ? 'Tạo phiếu xuất kho' : 'Sửa phiếu xuất kho' }}</v-card-title>
        <v-card-text>
          <v-form ref="form" @submit.prevent="saveOutward">
            <v-select
              v-model="formData.product_id"
              :items="products"
              item-title="product_name"
              item-value="product_id"
              label="Sản phẩm"
              variant="outlined"
              required
            ></v-select>
            <v-select
              v-model="formData.stock_id"
              :items="stocks"
              item-title="stock_name"
              item-value="stock_id"
              label="Kho"
              variant="outlined"
              required
            ></v-select>
            <v-text-field v-model="formData.quantity" label="Số lượng" variant="outlined" type="number" required></v-text-field>
            <v-text-field v-model="formData.unit_price" label="Đơn giá" variant="outlined" type="number" required></v-text-field>
            <v-text-field v-model="formData.outward_date" label="Ngày xuất" variant="outlined" type="date"></v-text-field>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="dialog = false">Hủy</v-btn>
          <v-btn color="primary" @click="saveOutward" :loading="saving">
            {{ mode === 'create' ? 'Tạo phiếu xuất' : 'Cập nhật' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation -->
    <v-dialog v-model="deleteDialog" max-width="400">
      <v-card>
        <v-card-title>Xác nhận xóa</v-card-title>
        <v-card-text>Bạn có chắc chắn muốn xóa phiếu xuất này?</v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="deleteDialog = false">Hủy</v-btn>
          <v-btn color="error" @click="deleteOutward" :loading="deleting">Xóa</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useOutwardStore } from '@/stores/outward'
import { useProductStore } from '@/stores/product'
import { useStockStore } from '@/stores/stock'
import { formatDate } from '@/utils/date'

const outwardStore = useOutwardStore()
const productStore = useProductStore()
const stockStore = useStockStore()

const search = ref('')
const dialog = ref(false)
const saving = ref(false)
const form = ref(null)
const mode = ref('create')
const editingId = ref(null)

const deleteDialog = ref(false)
const deleting = ref(false)
const selectedItem = ref(null)

const products = ref([])
const stocks = ref([])

const headers = [
  { title: 'Mã đơn', key: 'order_code' },
  { title: 'Sản phẩm', key: 'product_name' },
  { title: 'Kho', key: 'stock_name' },
  { title: 'Số lượng', key: 'quantity' },
  { title: 'Đơn giá', key: 'unit_price' },
  { title: 'Thành tiền', key: 'total' },
  { title: 'Ngày xuất', key: 'outward_date' },
  { title: 'Thao tác', key: 'actions', sortable: false }
]

const formData = ref({
  product_id: '',
  stock_id: '',
  quantity: 0,
  unit_price: 0,
  outward_date: ''
})

const formatCurrency = (value) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)
}

onMounted(async () => {
  await Promise.all([
    outwardStore.fetchAll(),
    productStore.fetchAll(),
    stockStore.fetchAll()
  ])
  products.value = productStore.products
  stocks.value = stockStore.stocks
})

const openDialog = () => {
  mode.value = 'create'
  editingId.value = null
  formData.value = {
    product_id: '',
    stock_id: '',
    quantity: 0,
    unit_price: 0,
    outward_date: new Date().toISOString().split('T')[0]
  }
  dialog.value = true
}

const openEditDialog = (item) => {
  // Phòng trường hợp bypass UI - validate ở frontend
  if (item.order_id) {
    alert('Không thể sửa phiếu xuất gắn với đơn hàng')
    return
  }
  mode.value = 'edit'
  editingId.value = item.outward_id
  formData.value = {
    product_id: item.product_id,
    stock_id: item.stock_id,
    quantity: item.quantity,
    unit_price: item.unit_price,
    outward_date: item.outward_date ? String(item.outward_date).split('T')[0] : ''
  }
  dialog.value = true
}

const saveOutward = async () => {
  try {
    saving.value = true
    if (mode.value === 'create') {
      await outwardStore.create(formData.value)
    } else {
      await outwardStore.update(editingId.value, formData.value)
    }
    dialog.value = false
  } catch (err) {
    console.error('Failed to save outward:', err)
    alert(err.response?.data?.message || 'Lỗi khi lưu phiếu xuất')
  } finally {
    saving.value = false
  }
}

const confirmDelete = (item) => {
  if (item.order_id) {
    alert('Phiếu xuất gắn với đơn hàng không thể xóa')
    return
  }
  selectedItem.value = item
  deleteDialog.value = true
}

const deleteOutward = async () => {
  try {
    deleting.value = true
    await outwardStore.remove(selectedItem.value.outward_id)
    deleteDialog.value = false
  } catch (err) {
    console.error('Failed to delete outward:', err)
    alert(err.response?.data?.message || 'Lỗi khi xóa phiếu xuất')
  } finally {
    deleting.value = false
  }
}
</script>