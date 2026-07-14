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
            <v-btn color="primary" data-testid="btn-open-create" @click="openDialog()">
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
        <template v-slot:item.order_date="{ item }">
          {{ formatDate(item.order_date) }}
        </template>
        <template v-slot:item.actions="{ item }">
          <v-icon size="small" class="mr-2" @click="openDetailDialog(item)">mdi-pencil</v-icon>
          <v-icon size="small" @click="confirmDelete(item)">mdi-delete</v-icon>
        </template>
      </v-data-table>
    </v-card>

    <!-- Create Order Dialog -->
    <v-dialog v-model="dialog" max-width="800" eager>
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

            <v-select
              v-model="formData.stock_id"
              :items="stocks"
              item-title="stock_name"
              item-value="stock_id"
              label="Kho"
              variant="outlined"
              data-testid="stock-select-create"
              @update:model-value="onCreateStockChange"
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
                  <span class="text-caption" :class="getStock(item.product_id) > 0 ? 'text-success' : 'text-error'">
                    Tồn: {{ getStock(item.product_id) }}
                  </span>
                </v-list-item-subtitle>
                <template v-slot:append>
                  <div class="d-flex align-center" style="gap: 8px;">
                    <v-text-field
                      :model-value="item.quantity"
                      :data-testid="`item-row-qty-${index}`"
                      label="SL"
                      density="compact"
                      variant="outlined"
                      type="number"
                      min="1"
                      :max="getMaxQuantity(item.product_id)"
                      hide-details
                      style="width: 90px;"
                      @update:model-value="(v) => updateItemField(item, 'quantity', v)"
                    ></v-text-field>
                    <v-text-field
                      :model-value="item.unit_price"
                      :data-testid="`item-row-price-${index}`"
                      label="Giá"
                      density="compact"
                      variant="outlined"
                      type="number"
                      min="0"
                      hide-details
                      style="width: 120px;"
                      @update:model-value="(v) => updateItemField(item, 'unit_price', v)"
                    ></v-text-field>
                    <span class="text-caption ml-1" style="min-width: 90px;">
                      = {{ formatCurrency(item.quantity * item.unit_price) }}
                    </span>
                    <v-btn icon size="small" color="error" @click="removeItem(index)">
                      <v-icon>mdi-delete</v-icon>
                    </v-btn>
                  </div>
                </template>
              </v-list-item>
            </v-list>

            <v-card variant="outlined" class="mb-3">
              <v-card-text>
                <v-row>
                  <v-col cols="12" sm="4">
                    <v-select
                      v-model="selectedProduct"
                      :items="availableProducts"
                      item-title="display_name"
                      item-value="product_id"
                      label="Sản phẩm"
                      variant="outlined"
                      density="compact"
                      return-object
                      @update:model-value="onProductChange"
                    ></v-select>
                  </v-col>
                  <v-col cols="6" sm="2">
                    <v-text-field
                      v-model="itemQuantity"
                      label="Số lượng"
                      variant="outlined"
                      density="compact"
                      type="number"
                      min="1"
                      :max="getMaxQuantity(selectedProduct?.product_id)"
                      data-testid="item-qty-input"
                      @keydown="onCreateQtyKey"
                      @keydown.enter="addItem"
                    ></v-text-field>
                  </v-col>
                  <v-col cols="6" sm="2">
                    <v-text-field
                      v-model="itemPrice"
                      label="Đơn giá"
                      variant="outlined"
                      density="compact"
                      type="number"
                    ></v-text-field>
                  </v-col>
                  <v-col cols="12" sm="2" class="d-flex align-center">
                    <span v-if="selectedProduct" class="text-caption" :class="currentStock > 0 ? 'text-success' : 'text-error'">
                      Tồn: {{ currentStock }}
                    </span>
                  </v-col>
                  <v-col cols="12" sm="2" class="d-flex align-center">
                    <v-btn color="primary" @click="addItem" :disabled="!canAddItem">
                      <v-icon>mdi-plus</v-icon>
                      Thêm
                    </v-btn>
                  </v-col>
                </v-row>
              </v-card-text>
            </v-card>

            <v-divider class="my-4"></v-divider>
            <div class="text-right">
              <strong>Tổng tiền: {{ formatCurrency(calculateTotal) }}</strong>
            </div>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="dialog = false">Hủy</v-btn>
          <v-btn color="primary" @click="saveOrder" :loading="saving">
            Tạo đơn hàng
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Order Detail Dialog -->
    <v-dialog v-model="detailDialog" max-width="700" eager>
      <v-card>
        <v-card-title>Chi tiết đơn hàng</v-card-title>
        <v-card-text>
          <v-form ref="detailForm">
            <v-select
              v-model="detailData.customer_id"
              :items="customers"
              item-title="full_name"
              item-value="customer_id"
              label="Khách hàng"
              variant="outlined"
            ></v-select>

            <v-select
              v-model="detailData.stock_id"
              :items="stocks"
              item-title="stock_name"
              item-value="stock_id"
              label="Kho"
              variant="outlined"
              data-testid="stock-select-edit"
              @update:model-value="onEditStockChange"
            ></v-select>

            <v-text-field
              :model-value="formatDate(detailData.order_date)"
              label="Ngày đặt"
              variant="outlined"
              readonly
              disabled
            ></v-text-field>

            <v-divider class="my-4"></v-divider>
            <h4>Chi tiết đơn hàng</h4>

            <v-list v-if="detailData.items && detailData.items.length > 0" class="mb-3">
              <v-list-item v-for="(item, index) in detailData.items" :key="item.product_id">
                <template v-slot:prepend>
                  <span class="mr-2">{{ index + 1 }}.</span>
                </template>
                <v-list-item-title>{{ getProductName(item.product_id) }}</v-list-item-title>
                <v-list-item-subtitle>
                  Thành tiền: <strong>{{ formatCurrency(item.quantity * item.unit_price) }}</strong>
                </v-list-item-subtitle>
                <template v-slot:append>
                  <div class="d-flex align-center" style="gap: 8px;">
                    <v-text-field
                      :model-value="item.quantity"
                      :data-testid="`detail-item-qty-${item.product_id}`"
                      label="SL"
                      density="compact"
                      variant="outlined"
                      type="number"
                      min="1"
                      :max="getMaxQuantity(item.product_id)"
                      hide-details
                      style="width: 90px;"
                      @keydown="onDetailQtyKey($event, item)"
                      @update:model-value="(v) => updateItemField(item, 'quantity', v)"
                    ></v-text-field>
                    <v-text-field
                      :model-value="item.unit_price"
                      :data-testid="`detail-item-price-${item.product_id}`"
                      label="Giá"
                      density="compact"
                      variant="outlined"
                      type="number"
                      min="0"
                      hide-details
                      style="width: 120px;"
                      @update:model-value="(v) => updateItemField(item, 'unit_price', v)"
                    ></v-text-field>
                    <v-btn icon size="small" color="error" @click="removeDetailItem(index)">
                      <v-icon>mdi-delete</v-icon>
                    </v-btn>
                  </div>
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
                <v-btn icon color="primary" @click="addDetailItem">
                  <v-icon>mdi-plus</v-icon>
                </v-btn>
              </v-col>
            </v-row>

            <v-divider class="my-4"></v-divider>
            <div class="text-right">
              <strong>Tổng tiền: {{ formatCurrency(detailTotal) }}</strong>
            </div>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="closeDetailDialog">Hủy</v-btn>
          <v-btn color="primary" data-testid="btn-save-edit" @click="saveEdit" :loading="saving" :disabled="!detailData.items || detailData.items.length === 0">
            Lưu
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation -->
    <v-dialog v-model="deleteDialog" max-width="400">
      <v-card>
        <v-card-title>Xác nhận xóa</v-card-title>
        <v-card-text>Bạn có chắc chắn muốn xóa đơn hàng này?</v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="deleteDialog = false">Hủy</v-btn>
          <v-btn color="error" @click="deleteOrder" :loading="deleting">Xóa</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useOrderStore } from '@/stores/order'
import { useCustomerStore } from '@/stores/customer'
import { useProductStore } from '@/stores/product'
import { useStockStore } from '@/stores/stock'
import { productPricesApi } from '@/api/client'
import { formatDate } from '@/utils/date'

const orderStore = useOrderStore()
const customerStore = useCustomerStore()
const productStore = useProductStore()
const stockStore = useStockStore()

const search = ref('')
const dialog = ref(false)
const detailDialog = ref(false)
const deleteDialog = ref(false)
const saving = ref(false)
const deleting = ref(false)
const form = ref(null)
const selectedItem = ref(null)

const customers = ref([])
const products = ref([])
const stocks = ref([])

const selectedProduct = ref(null)
const itemQuantity = ref(1)
const itemPrice = ref(0)
const currentStock = ref(0)
const productStocks = ref({})

const detailData = ref({
  order_id: null,
  customer_id: '',
  stock_id: '',
  order_code: '',
  total_amount: 0,
  status: '',
  order_date: '',
  items: []
})

const headers = [
  { title: 'Mã đơn', key: 'order_code' },
  { title: 'Khách hàng', key: 'customer_name' },
  { title: 'Kho', key: 'stock_name' },
  { title: 'Ngày đặt', key: 'order_date' },
  { title: 'Tổng tiền', key: 'total_amount' },
  { title: 'Thao tác', key: 'actions', sortable: false }
]

const formData = ref({
  customer_id: '',
  stock_id: '',
  items: []
})

const formatCurrency = (value) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value || 0)
}

const calculateTotal = computed(() => {
  return formData.value.items.reduce((sum, item) => sum + (item.quantity * item.unit_price), 0)
})

const detailTotal = computed(() => {
  return detailData.value.items?.reduce((sum, item) => sum + (item.quantity * item.unit_price), 0) || 0
})

const getProductName = (productId) => {
  const product = products.value.find(p => p.product_id === productId)
  return product?.product_name || productId
}

const availableProducts = computed(() => {
  return products.value.map(p => ({
    ...p,
    display_name: `${p.product_name} ${productStocks.value[p.product_id] !== undefined ? `(Tồn: ${productStocks.value[p.product_id] || 0})` : ''}`
  }))
})

const canAddItem = computed(() => {
  return selectedProduct.value && itemQuantity.value > 0 && itemPrice.value > 0 && itemQuantity.value <= getMaxQuantity(selectedProduct.value?.product_id)
})

const getStock = (productId) => {
  return productStocks.value[productId] || 0
}

const getMaxQuantity = (productId) => {
  const stock = productStocks.value[productId]
  if (stock === undefined) return 999
  return stock
}

const onProductChange = async (product) => {
  if (product) {
    // Fetch selling price
    try {
      const response = await productPricesApi.getSellingPrice(product.product_id)
      if (response.data.price > 0) {
        itemPrice.value = response.data.price
      }
    } catch (err) {
      console.error('Failed to fetch selling price:', err)
    }

    // Fetch stock
    if (formData.value.stock_id) {
      await fetchStock(product.product_id, formData.value.stock_id)
    }
  }
}

const fetchStock = async (productId, stockId) => {
  try {
    const response = await productPricesApi.getStock(productId, stockId)
    productStocks.value[productId] = response.data.quantity
    currentStock.value = response.data.quantity
  } catch (err) {
    console.error('Failed to fetch stock:', err)
    productStocks.value[productId] = 0
    currentStock.value = 0
  }
}

const refetchAllStocks = async (stockId) => {
  if (!stockId) return
  for (const product of products.value) {
    await fetchStock(product.product_id, stockId)
  }
}

const onCreateStockChange = async (stockId) => {
  await refetchAllStocks(stockId)
}

const onEditStockChange = async (stockId) => {
  await refetchAllStocks(stockId)
}

const onCreateQtyKey = (event) => {
  if (event.key !== 'ArrowUp' && event.key !== 'ArrowDown') return
  event.preventDefault()
  const maxQty = getMaxQuantity(selectedProduct.value?.product_id)
  const current = Number(itemQuantity.value) || 0
  const delta = event.key === 'ArrowUp' ? 1 : -1
  itemQuantity.value = clampQty(current + delta, maxQty)
}

const addItem = () => {
  if (selectedProduct.value && itemQuantity.value > 0 && itemPrice.value > 0) {
    // Check stock
    const availableQty = productStocks.value[selectedProduct.value.product_id] || 0
    if (itemQuantity.value > availableQty) {
      alert(`Số lượng vượt quá tồn kho! Tồn kho: ${availableQty}`)
      return
    }

    formData.value.items.push({
      product_id: selectedProduct.value.product_id,
      quantity: itemQuantity.value,
      unit_price: itemPrice.value
    })

    // Deduct from stock display
    productStocks.value[selectedProduct.value.product_id] = availableQty - itemQuantity.value

    selectedProduct.value = null
    itemQuantity.value = 1
    itemPrice.value = 0
    currentStock.value = 0
  }
}

const removeItem = (index) => {
  const item = formData.value.items[index]
  // Restore stock
  productStocks.value[item.product_id] = (productStocks.value[item.product_id] || 0) + item.quantity
  formData.value.items.splice(index, 1)
}

const addDetailItem = () => {
  if (selectedProduct.value && itemQuantity.value > 0 && itemPrice.value > 0) {
    detailData.value.items.push({
      product_id: selectedProduct.value,
      quantity: itemQuantity.value,
      unit_price: itemPrice.value
    })
    selectedProduct.value = null
    itemQuantity.value = 1
    itemPrice.value = 0
  }
}

const removeDetailItem = (index) => {
  detailData.value.items.splice(index, 1)
}

const updateItemField = (item, field, value) => {
  const num = Number(value)
  if (!Number.isFinite(num)) return
  if (field === 'quantity') {
    const oldQty = item.quantity || 0
    const newQty = Math.max(0, num)
    item.quantity = newQty
    // Đồng bộ productStocks: hoàn lại qty cũ, trừ qty mới
    const stockKey = item.product_id
    const base = productStocks.value[stockKey] || 0
    productStocks.value[stockKey] = base + oldQty - newQty
  } else {
    item[field] = num
  }
}

const clampQty = (value, maxQty) => {
  if (!Number.isFinite(value)) return 1
  const min = 1
  const max = Number.isFinite(maxQty) ? maxQty : 999
  return Math.max(min, Math.min(max, value))
}

const onDetailQtyKey = (event, item) => {
  if (event.key !== 'ArrowUp' && event.key !== 'ArrowDown') return
  event.preventDefault()
  const maxQty = getMaxQuantity(item.product_id)
  const current = Number(item.quantity) || 0
  const delta = event.key === 'ArrowUp' ? 1 : -1
  item.quantity = clampQty(current + delta, maxQty)
}

onMounted(async () => {
  await Promise.all([
    orderStore.fetchAll(),
    customerStore.fetchAll(),
    productStore.fetchAll(),
    stockStore.fetchAll()
  ])
  customers.value = customerStore.customers
  products.value = productStore.products
  stocks.value = stockStore.stocks

  // Set default stock if available
  if (stocks.value.length > 0) {
    formData.value.stock_id = stocks.value[0].stock_id
  }

  // Fetch stock for all products
  for (const product of products.value) {
    if (stocks.value.length > 0) {
      await fetchStock(product.product_id, stocks.value[0].stock_id)
    }
  }
})

watch([dialog, detailDialog], ([newDialog, newDetailDialog]) => {
  if (newDialog || newDetailDialog) {
    document.addEventListener('keydown', handleKeydown)
  } else {
    document.removeEventListener('keydown', handleKeydown)
  }
})

const handleKeydown = (e) => {
  if (e.key === '+' || e.key === 'k' || e.key === 'K') {
    if (dialog.value) {
      addItem()
    } else if (detailDialog.value) {
      addDetailItem()
    }
  }
}

const openDialog = () => {
  const defaultStockId = stocks.value.length > 0 ? stocks.value[0].stock_id : ''
  formData.value = {
    customer_id: customers.value.length > 0 ? customers.value[0].customer_id : '',
    stock_id: defaultStockId,
    items: []
  }
  productStocks.value = {}
  if (defaultStockId) {
    refetchAllStocks(defaultStockId)
  }
  dialog.value = true
}

const saveOrder = async () => {
  // Check if current input row is valid and add it if so
  if (selectedProduct.value && itemQuantity.value > 0 && itemPrice.value > 0) {
    const availableQty = productStocks.value[selectedProduct.value.product_id] || 0
    if (itemQuantity.value > availableQty) {
      alert(`Số lượng vượt quá tồn kho! Tồn kho: ${availableQty}`)
      return
    }
    // Add current row to items
    formData.value.items.push({
      product_id: selectedProduct.value.product_id,
      quantity: itemQuantity.value,
      unit_price: itemPrice.value
    })
    productStocks.value[selectedProduct.value.product_id] = availableQty - itemQuantity.value
  }

  if (formData.value.items.length === 0) {
    alert('Vui lòng thêm ít nhất một sản phẩm vào đơn hàng')
    return
  }

  // Check stock before saving
  for (const item of formData.value.items) {
    const availableQty = productStocks.value[item.product_id] || 0
    if (item.quantity > availableQty) {
      const productName = getProductName(item.product_id)
      alert(`Sản phẩm "${productName}" vượt quá tồn kho! Tồn kho: ${availableQty}`)
      return
    }
  }

  try {
    saving.value = true
    const orderData = {
      customer_id: formData.value.customer_id,
      stock_id: formData.value.stock_id,
      items: formData.value.items.map(item => ({
        product_id: item.product_id,
        quantity: item.quantity,
        unit_price: item.unit_price
      }))
    }
    await orderStore.create(orderData)
    dialog.value = false
    selectedProduct.value = null
    itemQuantity.value = 1
    itemPrice.value = 0
  } catch (err) {
    console.error('Failed to create order:', err)
  } finally {
    saving.value = false
  }
}

const openDetailDialog = async (item) => {
  try {
    const order = await orderStore.getById(item.order_id)
    const defaultStockId = order.stock_id || (stocks.value.length > 0 ? stocks.value[0].stock_id : '')
    detailData.value = {
      order_id: order.order_id,
      customer_id: order.customer_id,
      stock_id: defaultStockId,
      order_code: order.order_code,
      total_amount: order.total_amount,
      status: order.status,
      order_date: order.order_date,
      items: order.items || []
    }
    detailDialog.value = true
    if (defaultStockId) {
      await refetchAllStocks(defaultStockId)
    }
  } catch (err) {
    console.error('Failed to load order:', err)
  }
}

const closeDetailDialog = () => {
  detailDialog.value = false
  selectedProduct.value = null
  itemQuantity.value = 1
  itemPrice.value = 0
}

const saveEdit = async () => {
  try {
    saving.value = true
    const { order_id, order_code, total_amount, status, ...data } = detailData.value
    data.items = detailData.value.items.map(item => ({
      product_id: item.product_id,
      quantity: item.quantity,
      unit_price: item.unit_price
    }))
    await orderStore.update(order_id, data)
    detailDialog.value = false
  } catch (err) {
    console.error('Failed to update order:', err)
  } finally {
    saving.value = false
  }
}

const confirmDelete = (item) => {
  selectedItem.value = item
  deleteDialog.value = true
}

const deleteOrder = async () => {
  try {
    deleting.value = true
    await orderStore.remove(selectedItem.value.order_id)
    deleteDialog.value = false
  } catch (err) {
    console.error('Failed to delete order:', err)
    alert(err.response?.data?.message || 'Lỗi khi xóa đơn hàng')
  } finally {
    deleting.value = false
  }
}
</script>
