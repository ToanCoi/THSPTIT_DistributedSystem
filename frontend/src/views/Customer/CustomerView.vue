<template>
  <div>
    <v-row class="mb-4">
      <v-col cols="12">
        <h2 class="text-h4">Quản lý Khách hàng</h2>
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
              Thêm khách hàng
            </v-btn>
          </v-col>
        </v-row>
      </v-card-title>

      <v-data-table
        :headers="headers"
        :items="customerStore.customers"
        :search="search"
        :loading="customerStore.loading"
      >
        <template v-slot:item.actions="{ item }">
          <v-icon size="small" class="mr-2" @click="openDialog(item)">mdi-pencil</v-icon>
          <v-icon size="small" @click="confirmDelete(item)">mdi-delete</v-icon>
        </template>
      </v-data-table>
    </v-card>

    <!-- Customer Dialog -->
    <v-dialog v-model="dialog" max-width="500">
      <v-card>
        <v-card-title>{{ isEdit ? 'Sửa khách hàng' : 'Thêm khách hàng' }}</v-card-title>
        <v-card-text>
          <v-form ref="form" @submit.prevent="saveCustomer">
            <v-text-field v-model="formData.full_name" label="Họ và tên" variant="outlined" required></v-text-field>
            <v-text-field v-model="formData.phone" label="Số điện thoại" variant="outlined"></v-text-field>
            <v-text-field v-model="formData.email" label="Email" variant="outlined" type="email"></v-text-field>
            <v-textarea v-model="formData.address" label="Địa chỉ" variant="outlined" rows="2"></v-textarea>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="dialog = false">Hủy</v-btn>
          <v-btn color="primary" @click="saveCustomer" :loading="saving">{{ isEdit ? 'Lưu' : 'Thêm' }}</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation -->
    <v-dialog v-model="deleteDialog" max-width="400">
      <v-card>
        <v-card-title>Xác nhận xóa</v-card-title>
        <v-card-text>Bạn có chắc chắn muốn xóa khách hàng này?</v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn variant="text" @click="deleteDialog = false">Hủy</v-btn>
          <v-btn color="error" @click="deleteCustomer" :loading="deleting">Xóa</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useCustomerStore } from '@/stores/customer'

const customerStore = useCustomerStore()

const search = ref('')
const dialog = ref(false)
const deleteDialog = ref(false)
const isEdit = ref(false)
const saving = ref(false)
const deleting = ref(false)
const form = ref(null)
const selectedItem = ref(null)

const headers = [
  { title: 'Họ và tên', key: 'full_name' },
  { title: 'Số điện thoại', key: 'phone' },
  { title: 'Email', key: 'email' },
  { title: 'Địa chỉ', key: 'address' },
  { title: 'Thao tác', key: 'actions', sortable: false }
]

const formData = ref({
  full_name: '',
  phone: '',
  email: '',
  address: ''
})

onMounted(() => {
  customerStore.fetchAll()
})

const openDialog = (item = null) => {
  if (item) {
    isEdit.value = true
    selectedItem.value = item
    formData.value = { ...item }
  } else {
    isEdit.value = false
    selectedItem.value = null
    formData.value = { full_name: '', phone: '', email: '', address: '' }
  }
  dialog.value = true
}

const saveCustomer = async () => {
  try {
    saving.value = true
    if (isEdit.value) {
      await customerStore.update(selectedItem.value.customer_id, formData.value)
    } else {
      await customerStore.create(formData.value)
    }
    dialog.value = false
  } catch (err) {
    console.error('Failed to save customer:', err)
  } finally {
    saving.value = false
  }
}

const confirmDelete = (item) => {
  selectedItem.value = item
  deleteDialog.value = true
}

const deleteCustomer = async () => {
  try {
    deleting.value = true
    await customerStore.remove(selectedItem.value.customer_id)
    deleteDialog.value = false
  } catch (err) {
    console.error('Failed to delete customer:', err)
  } finally {
    deleting.value = false
  }
}
</script>