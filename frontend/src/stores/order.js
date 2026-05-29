import { defineStore } from 'pinia'
import { ref } from 'vue'
import { orderApi } from '@/api/client'

export const useOrderStore = defineStore('order', () => {
  const orders = ref([])
  const loading = ref(false)
  const error = ref(null)

  const fetchAll = async () => {
    loading.value = true
    error.value = null
    try {
      const response = await orderApi.getAll()
      orders.value = response.data
    } catch (err) {
      error.value = err.message
      console.error('Failed to fetch orders:', err)
    } finally {
      loading.value = false
    }
  }

  const create = async (data) => {
    const response = await orderApi.create(data)
    await fetchAll()
    return response.data
  }

  return {
    orders,
    loading,
    error,
    fetchAll,
    create
  }
})