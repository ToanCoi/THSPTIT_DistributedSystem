import { defineStore } from 'pinia'
import { ref } from 'vue'
import { inwardApi } from '@/api/client'

export const useInwardStore = defineStore('inward', () => {
  const inwards = ref([])
  const loading = ref(false)
  const error = ref(null)

  const fetchAll = async () => {
    loading.value = true
    error.value = null
    try {
      const response = await inwardApi.getAll()
      inwards.value = response.data
    } catch (err) {
      error.value = err.message
      console.error('Failed to fetch inwards:', err)
    } finally {
      loading.value = false
    }
  }

  const create = async (data) => {
    const response = await inwardApi.create(data)
    await fetchAll()
    return response.data
  }

  return {
    inwards,
    loading,
    error,
    fetchAll,
    create
  }
})