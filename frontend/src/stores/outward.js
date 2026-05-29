import { defineStore } from 'pinia'
import { ref } from 'vue'
import { outwardApi } from '@/api/client'

export const useOutwardStore = defineStore('outward', () => {
  const outwards = ref([])
  const loading = ref(false)
  const error = ref(null)

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

  const create = async (data) => {
    const response = await outwardApi.create(data)
    await fetchAll()
    return response.data
  }

  return {
    outwards,
    loading,
    error,
    fetchAll,
    create
  }
})