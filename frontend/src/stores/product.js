import { defineStore } from 'pinia'
import { ref } from 'vue'
import { productApi } from '@/api/client'

export const useProductStore = defineStore('product', () => {
  const products = ref([])
  const loading = ref(false)
  const error = ref(null)

  const fetchAll = async () => {
    loading.value = true
    error.value = null
    try {
      const response = await productApi.getAll()
      products.value = response.data
    } catch (err) {
      error.value = err.message
      console.error('Failed to fetch products:', err)
    } finally {
      loading.value = false
    }
  }

  const create = async (data) => {
    const response = await productApi.create(data)
    await fetchAll()
    return response.data
  }

  const update = async (id, data) => {
    const response = await productApi.update(id, data)
    await fetchAll()
    return response.data
  }

  const remove = async (id) => {
    await productApi.delete(id)
    await fetchAll()
  }

  return {
    products,
    loading,
    error,
    fetchAll,
    create,
    update,
    remove
  }
})