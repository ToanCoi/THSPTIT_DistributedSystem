import { defineStore } from 'pinia'
import { ref } from 'vue'
import { stockApi } from '@/api/client'

export const useStockStore = defineStore('stock', () => {
  const stocks = ref([])
  const loading = ref(false)
  const error = ref(null)

  const fetchAll = async () => {
    loading.value = true
    error.value = null
    try {
      const response = await stockApi.getAll()
      stocks.value = response.data
    } catch (err) {
      error.value = err.message
      console.error('Failed to fetch stocks:', err)
    } finally {
      loading.value = false
    }
  }

  return {
    stocks,
    loading,
    error,
    fetchAll
  }
})