import { defineStore } from 'pinia'
import { ref } from 'vue'
import { customerApi } from '@/api/client'

export const useCustomerStore = defineStore('customer', () => {
  const customers = ref([])
  const loading = ref(false)
  const error = ref(null)

  const fetchAll = async () => {
    loading.value = true
    error.value = null
    try {
      const response = await customerApi.getAll()
      customers.value = response.data
    } catch (err) {
      error.value = err.message
      console.error('Failed to fetch customers:', err)
    } finally {
      loading.value = false
    }
  }

  const create = async (data) => {
    const response = await customerApi.create(data)
    await fetchAll()
    return response.data
  }

  const update = async (id, data) => {
    const response = await customerApi.update(id, data)
    await fetchAll()
    return response.data
  }

  const remove = async (id) => {
    await customerApi.delete(id)
    await fetchAll()
  }

  return {
    customers,
    loading,
    error,
    fetchAll,
    create,
    update,
    remove
  }
})