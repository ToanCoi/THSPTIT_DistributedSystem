<template>
  <div>
    <v-row class="mb-4">
      <v-col cols="12">
        <h2 class="text-h4">Quản lý Đơn hàng</h2>
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
              Tạo đơn hàng
            </v-btn>
          </v-col>
        </v-row>
      </v-card-title>

      <v-data-table
        :headers="headers"
        :items="orderStore.orders"
        :search="search"
        :loading="orderStore.loading"
      >
        <template v-slot:item.total_amount="{ item }">
          {{ formatCurrency(item.total_amount) }}
        </template>
        <template v-slot:item.status="{ item }">
          <v-chip :color="getStatusColor(item.status)" size="small">
            {{ item.status }}
          </v-chip>
        </template>
      </v-data-table>
    </v-card>

    <!-- Order Dialog -->
    <v-dialog v-model="dialog" max-width="700">
      <v-card>
        <v-card-title>Tạo đơn hàng mới</v-card-title>
        <v-card-text>
          <v-form ref="form" @submit.prevent="saveOrder">
            <v-select
              v-model="formData.customer_id"
              :items="customers"
              item-title="full_name"
              item-value="customer_id"
              label="Khách hàng"
              variant="outlined"
              required
            ></v-select>

            <v-divider class="my-4"></v-divider>
            <h4>Chi tiết đơn hàng</h4>

            <v-list v-if="formData.items.length > 0" class="mb-3">
              <v-list-item v-for="(item, index) in formData.items" :key="index">
                <template v-slot:prepend>
                  <span class="mr-2">{{ index + 1 }}.</span>
                </template>
                <v-list-item-title>{{ getProductName(item.product_id) }}</v-list-item-title>
                <v-list-item-subtitle>
                  {{ item.quantity }} x {{ formatCurrency(item.unit_price) }}
                </v-list-item-subtitle>
                <template v-slot:append>
                  <v-btn icon size="small" color="error" @click="removeItem(index)">
                    <v-icon>mdi-delete</v-icon>
                  </v-btn>
                </template>
              </v-list-item>
            </v-list>

            <v-row>
              <v-col cols="12" sm="5">
                <v-select
                  v-model="selectedProduct"
                  :items="products"
                  item-title="product_name"
                  item-value="product_id"
                  label="Sản phẩm"
                  variant="outlined"
                  density="compact"
                ></v-select>
              </v-col>
              <v-col cols="6" sm="3">
                <v-text-field
                  v-model="itemQuantity"
                  label="Số lượng"
                  variant="outlined"
                  density="compact"
                  type="number"
                ></v-text-field>
              </v-col>
              <v-col cols="6" sm="3">
                <v-text-field
                  v-model="itemPrice"
                  label="Đơn giá"
                  variant="outlined"
                  density="compact"
                  type="number"
                ></v-text-field>
              </v-col>
              <v-col cols="12" sm="1" class="d-flex align-center">
                <v-btn icon color="primary" @click="addItem">
                  <v-icon>mdi-plus</v-icon>
                </v-btn>
              </v-col>
            </v-row>

            <v-divider class="my-4"></v-divider>
            <div class="text-right">
              <strong>Tổng tiền: {{ formatCurrency(calculateTotal) }}</strong>
            </div>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="dialog = false">Hủy</v-btn>
          <v-btn color="primary" @click="saveOrder" :loading="saving" :disabled="formData.items.length === 0">
            Tạo đơn hàng
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useOrderStore } from '@/stores/order'
import { useCustomerStore } from '@/stores/customer'
import { useProductStore } from '@/stores/product'

const orderStore = useOrderStore()
const customerStore = useCustomerStore()
const productStore = useProductStore()

const search = ref('')
const dialog = ref(false)
const saving = ref(false)
const form = ref(null)

const customers = ref([])
const products = ref([])

const selectedProduct = ref(null)
const itemQuantity = ref(1)
const itemPrice = ref(0)

const headers = [
  { title: 'Mã đơn', key: 'order_code' },
  { title: 'Khách hàng', key: 'customer_name' },
  { title: 'Ngày đặt', key: 'order_date' },
  { title: 'Tổng tiền', key: 'total_amount' },
  { title: 'Trạng thái', key: 'status' }
]

const formData = ref({
  customer_id: '',
  items: []
})

const formatCurrency = (value) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value || 0)
}

const getStatusColor = (status) => {
  const colors = {
    'pending': 'warning',
    'completed': 'success',
    'cancelled': 'error',
    'processing': 'info'
  }
  return colors[status?.toLowerCase()] || 'grey'
}

const calculateTotal = computed(() => {
  return formData.value.items.reduce((sum, item) => sum + (item.quantity * item.unit_price), 0)
})

const getProductName = (productId) => {
  const product = products.value.find(p => p.product_id === productId)
  return product?.product_name || productId
}

const addItem = () => {
  if (selectedProduct.value && itemQuantity.value > 0 && itemPrice.value > 0) {
    formData.value.items.push({
      product_id: selectedProduct.value,
      quantity: itemQuantity.value,
      unit_price: itemPrice.value
    })
    selectedProduct.value = null
    itemQuantity.value = 1
    itemPrice.value = 0
  }
}

const removeItem = (index) => {
  formData.value.items.splice(index, 1)
}

onMounted(async () => {
  await Promise.all([
    orderStore.fetchAll(),
    customerStore.fetchAll(),
    productStore.fetchAll()
  ])
  customers.value = customerStore.customers
  products.value = productStore.products
})

const openDialog = () => {
  formData.value = {
    customer_id: customers.value.length > 0 ? customers.value[0].customer_id : '',
    items: []
  }
  dialog.value = true
}

const saveOrder = async () => {
  try {
    saving.value = true
    await orderStore.create(formData.value)
    dialog.value = false
  } catch (err) {
    console.error('Failed to create order:', err)
  } finally {
    saving.value = false
  }
}
</script>