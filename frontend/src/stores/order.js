import { defineStore } from 'pinia'
import { ref } from 'vue'
import { orderApi } from '@/api/client'

export const useOrderStore = defineStore('order', () => {
  const orders = ref([])
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
      const response = await orderApi.getAll()
      orders.value = response.data
    } catch (err) {
      error.value = err.message
      console.error('Failed to fetch orders:', err)
    } finally {
      loading.value = false
    }
  }

  const fetchPaging = async (filter = { skip: 0, take: 20, sort_field: 'created_date', sort_order: 'DESC' }) => {
    loading.value = true
    error.value = null
    try {
      const response = await orderApi.getPaging(filter)
      orders.value = response.data.data
      pagination.value.total = response.data.total
      pagination.value.skip = filter.skip
      pagination.value.take = filter.take
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

  const getById = async (id) => {
    const response = await orderApi.getById(id)
    return response.data
  }

  const update = async (id, data) => {
    const response = await orderApi.update(id, data)
    await fetchAll()
    return response.data
  }

  return {
    orders,
    loading,
    error,
    pagination,
    fetchAll,
    fetchPaging,
    create,
    getById,
    update
  }
})
