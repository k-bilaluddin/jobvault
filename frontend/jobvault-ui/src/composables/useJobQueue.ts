import { ref, computed, onMounted } from 'vue'
import { api } from '@/api'

export interface PendingJob {
  jobId: string
  url: string
  status: 'pending' | 'done' | 'failed'
  createdAt: string
  updatedAt: string
}

export function useJobQueue() {
  const jobs = ref<PendingJob[]>([])
  const loading = ref(false)
  const filterStatus = ref<'all' | 'pending' | 'done' | 'failed'>('all')

  async function load() {
    loading.value = true
    try {
      const params = filterStatus.value !== 'all' ? { status: filterStatus.value } : {}
      const { data } = await api.get<PendingJob[]>('/api/ingest/queue', { params })
      jobs.value = data
    } finally {
      loading.value = false
    }
  }

  async function addJob(url: string) {
    const { data } = await api.post<PendingJob>('/api/ingest/queue', { url })
    jobs.value.unshift(data)
    return data
  }

  async function updateJob(id: string, url?: string, status?: string) {
    await api.put(`/api/ingest/queue/${id}`, { url, status })
    await load()
  }

  async function deleteJob(id: string) {
    await api.delete(`/api/ingest/queue/${id}`)
    jobs.value = jobs.value.filter(j => j.jobId !== id)
  }

  async function cleanup(status: string) {
    const { data } = await api.delete<{ deleted: number }>(`/api/ingest/queue/cleanup/${status}`)
    await load()
    return data.deleted
  }

  const counts = computed(() => {
    const all = jobs.value
    return {
      all: all.length,
      pending: all.filter(j => j.status === 'pending').length,
      done: all.filter(j => j.status === 'done').length,
      failed: all.filter(j => j.status === 'failed').length,
    }
  })

  onMounted(load)

  return { jobs, loading, filterStatus, counts, load, addJob, updateJob, deleteJob, cleanup }
}
