import { ref, computed, onMounted } from 'vue'
import type { Application, ApplicationStatus } from '@/types'
import { mockApplications } from '@/mocks/dashboard'

const USE_MOCK = import.meta.env.VITE_USE_MOCK !== 'false'
const API_BASE = import.meta.env.VITE_API_BASE ?? 'https://api.kbilaluddin.dev'

export function useApplications() {
  const all = ref<Application[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)
  const search = ref('')
  const filterStatus = ref<ApplicationStatus | 'All'>('All')

  async function fetch() {
    loading.value = true
    error.value = null
    try {
      if (USE_MOCK) {
        await new Promise(r => setTimeout(r, 300))
        all.value = mockApplications
      } else {
        const res = await window.fetch(`${API_BASE}/api/applications`)
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        all.value = await res.json()
      }
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
    } finally {
      loading.value = false
    }
  }

  const filtered = computed(() => {
    let list = all.value
    if (filterStatus.value !== 'All') {
      list = list.filter(a => a.status === filterStatus.value)
    }
    if (search.value.trim()) {
      const q = search.value.toLowerCase()
      list = list.filter(a =>
        a.company.toLowerCase().includes(q) ||
        a.role.toLowerCase().includes(q) ||
        a.location.toLowerCase().includes(q)
      )
    }
    return list
  })

  function getById(id: string) {
    return all.value.find(a => a.id === id) ?? null
  }

  onMounted(fetch)

  return { all, filtered, loading, error, search, filterStatus, refresh: fetch, getById }
}
