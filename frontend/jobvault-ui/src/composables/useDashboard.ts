import { ref, onMounted } from 'vue'
import type { DashboardData } from '@/types'
import { mockDashboard } from '@/mocks/dashboard'

const USE_MOCK = import.meta.env.VITE_USE_MOCK !== 'false'
const API_BASE = import.meta.env.VITE_API_BASE ?? 'https://api.kbilaluddin.dev'

export function useDashboard() {
  const data = ref<DashboardData | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetch() {
    loading.value = true
    error.value = null
    try {
      if (USE_MOCK) {
        // Simulate network latency
        await new Promise((r) => setTimeout(r, 400))
        data.value = mockDashboard
      } else {
        const res = await window.fetch(`${API_BASE}/api/dashboard`)
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        data.value = await res.json()
      }
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
    } finally {
      loading.value = false
    }
  }

  onMounted(fetch)

  return { data, loading, error, refresh: fetch }
}
