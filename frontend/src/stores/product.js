import { defineStore } from 'pinia'
import { ref } from 'vue'
import { productApi } from '@/api/client'

export const useProductStore = defineStore('product', () => {
  const products = ref([])
  const loading = ref(false)
  const error = ref(null)
  const pagination = ref({
    total: 0,
    skip: 0,
    take: 20
  })

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

  const fetchPaging = async (filter = { skip: 0, take: 20, sort_field: 'created_date', sort_order: 'DESC' }) => {
    loading.value = true
    error.value = null
    try {
      const response = await productApi.getPaging(filter)
      products.value = response.data.data
      pagination.value.total = response.data.total
      pagination.value.skip = filter.skip
      pagination.value.take = filter.take
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
    pagination,
    fetchAll,
    fetchPaging,
    create,
    update,
    remove
  }
})
