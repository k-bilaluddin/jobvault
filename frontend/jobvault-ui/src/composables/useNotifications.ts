import { ref, computed, onMounted } from 'vue'
import type { AppNotification } from '@/types'
import { forceRefreshCompanies } from '@/composables/useCompanies'
import { api } from '@/api'

const API_BASE = import.meta.env.VITE_NOTIFICATION_API_BASE ?? 'https://api.kbilaluddin.dev'

// Singleton state — shared across all useNotifications() instances
const _notifications = ref<AppNotification[]>([])
const _connected = ref(false)
const _loaded = ref(false)
const _loading = ref(false)
const _error = ref<string | null>(null)

let _eventSource: EventSource | null = null
let _reconnectTimer: ReturnType<typeof setTimeout> | null = null

function handleSseMessage(event: MessageEvent) {
  try {
    const notification: AppNotification = JSON.parse(event.data)
    // Prepend so newest is first
    _notifications.value = [notification, ..._notifications.value]
    // Auto-refresh companies when a new application or sync completes
    if (notification.type === 'new_application' || notification.type === 'sync_completed') {
      forceRefreshCompanies()
    }
  } catch {
    // Malformed SSE message — ignore
  }
}

function scheduleReconnect() {
  if (_reconnectTimer) return
  _reconnectTimer = setTimeout(() => {
    _reconnectTimer = null
    connect()
  }, 5000)
}

function connect() {
  if (_eventSource) {
    _eventSource.close()
    _eventSource = null
  }

  const token = localStorage.getItem('jv_token') ?? ''
  const source = new EventSource(`${API_BASE}/api/notifications/stream?token=${encodeURIComponent(token)}`)
  _eventSource = source

  source.onopen = () => {
    _connected.value = true
    _error.value = null
  }

  source.onmessage = handleSseMessage

  source.onerror = () => {
    _connected.value = false
    source.close()
    _eventSource = null
    scheduleReconnect()
  }
}

async function loadNotifications() {
  if (_loaded.value || _loading.value) return
  _loading.value = true
  _error.value = null
  try {
    const res = await api.get<AppNotification[]>('/api/notifications')
    _notifications.value = res.data
    _loaded.value = true
  } catch (e: unknown) {
    _error.value = e instanceof Error ? e.message : 'Unknown error'
  } finally {
    _loading.value = false
  }
}

export function useNotifications() {
  onMounted(loadNotifications)

  const unreadCount = computed(() => _notifications.value.filter(n => !n.read).length)

  async function markAllRead() {
    try {
      await api.post('/api/notifications/read-all')
      _notifications.value = _notifications.value.map(n => ({ ...n, read: true }))
    } catch (e: unknown) {
      _error.value = e instanceof Error ? e.message : 'Unknown error'
    }
  }

  async function markRead(id: string) {
    try {
      await api.post(`/api/notifications/${id}/read`)
      const n = _notifications.value.find(n => n.id === id)
      if (n) n.read = true
    } catch (e: unknown) {
      _error.value = e instanceof Error ? e.message : 'Unknown error'
    }
  }

  return {
    notifications: _notifications,
    unreadCount,
    connected: _connected,
    loading: _loading,
    error: _error,
    connect,
    markAllRead,
    markRead,
  }
}
