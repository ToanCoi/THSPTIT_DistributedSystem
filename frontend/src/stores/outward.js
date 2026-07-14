import { defineStore } from 'pinia'
import { ref } from 'vue'
import { outwardApi } from '@/api/client'

export const useOutwardStore = defineStore('outward', () => {
  const outwards = ref([])
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
      const response = await outwardApi.getAll()
      outwards.value = response.data
    } catch (err) {
      error.value = err.message
      console.error('Failed to fetch outwards:', err)
    } finally {
      loading.value = false
    }
  }

  const fetchPaging = async (filter = { skip: 0, take: 20, sort_field: 'created_date', sort_order: 'DESC' }) => {
    loading.value = true
    error.value = null
    try {
      const response = await outwardApi.getPaging(filter)
      outwards.value = response.data.data
      pagination.value.total = response.data.total
      pagination.value.skip = filter.skip
      pagination.value.take = filter.take
    } catch (err) {
      error.value = err.message
      console.error('Failed to fetch outwards:', err)
    } finally {
      loading.value = false
    }
  }

  const create = async (data) => {
    const response = await outwardApi.create(data)
    await fetchAll()
    return response.data
  }

  const update = async (id, data) => {
    const response = await outwardApi.update(id, data)
    await fetchAll()
    return response.data
  }

  const remove = async (id) => {
    await outwardApi.delete(id)
    await fetchAll()
  }

  return {
    outwards,
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
