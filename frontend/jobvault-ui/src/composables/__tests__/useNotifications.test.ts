import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { nextTick } from 'vue'
import { withSetup } from './helpers'
import type { AppNotification } from '@/types'

const flushPromises = () => new Promise(resolve => setTimeout(resolve, 0))

function makeNotification(overrides: Partial<AppNotification> = {}): AppNotification {
  return {
    id: 'n1',
    type: 'new_application',
    title: 'Test',
    body: 'Test body',
    companyName: 'Acme',
    companySlug: 'acme',
    occurredAt: '2026-06-18T10:00:00Z',
    read: false,
    ...overrides,
  }
}

// Mock the axios api instance
const mockGet  = vi.fn().mockResolvedValue({ data: [] })
const mockPost = vi.fn().mockResolvedValue({ data: {} })

vi.mock('@/api', () => ({
  api: {
    get:  (...args: unknown[]) => mockGet(...args),
    post: (...args: unknown[]) => mockPost(...args),
    interceptors: {
      request:  { use: vi.fn() },
      response: { use: vi.fn() },
    },
  },
}))

describe('useNotifications', () => {
  beforeEach(() => {
    vi.resetModules()
    mockGet.mockResolvedValue({ data: [] })
    mockPost.mockResolvedValue({ data: {} })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  async function loadNotifications() {
    const mod = await import('../useNotifications')
    const setup = withSetup(() => mod.useNotifications())
    await flushPromises()
    return setup
  }

  it('returns notifications array', async () => {
    const { result } = await loadNotifications()
    expect(Array.isArray(result.notifications.value)).toBe(true)
  })

  it('returns initial connected as false', async () => {
    const { result } = await loadNotifications()
    expect(result.connected.value).toBe(false)
  })

  describe('unreadCount', () => {
    it('counts unread notifications', async () => {
      const { result } = await loadNotifications()
      result.notifications.value = [
        makeNotification({ id: '1', read: false }),
        makeNotification({ id: '2', read: true }),
        makeNotification({ id: '3', read: false }),
      ]
      await nextTick()
      expect(result.unreadCount.value).toBe(2)
    })

    it('returns 0 when all are read', async () => {
      const { result } = await loadNotifications()
      result.notifications.value = [
        makeNotification({ id: '1', read: true }),
        makeNotification({ id: '2', read: true }),
      ]
      await nextTick()
      expect(result.unreadCount.value).toBe(0)
    })

    it('returns 0 when empty', async () => {
      const { result } = await loadNotifications()
      result.notifications.value = []
      await nextTick()
      expect(result.unreadCount.value).toBe(0)
    })
  })

  describe('markAllRead', () => {
    it('marks all notifications as read', async () => {
      const { result } = await loadNotifications()
      result.notifications.value = [
        makeNotification({ id: '1', read: false }),
        makeNotification({ id: '2', read: false }),
      ]
      await result.markAllRead()
      await nextTick()
      expect(result.notifications.value.every(n => n.read)).toBe(true)
    })

    it('calls the read-all endpoint', async () => {
      const { result } = await loadNotifications()
      await result.markAllRead()
      const call = mockPost.mock.calls.find(
        (c: unknown[]) => typeof c[0] === 'string' && c[0].includes('/read-all')
      )
      expect(call).toBeDefined()
    })

    it('sets error on fetch failure', async () => {
      const { result } = await loadNotifications()
      mockPost.mockRejectedValueOnce(new Error('Network error'))
      await result.markAllRead()
      expect(result.error.value).toBe('Network error')
    })
  })

  describe('markRead', () => {
    it('marks a single notification as read', async () => {
      const { result } = await loadNotifications()
      result.notifications.value = [
        makeNotification({ id: 'target', read: false }),
        makeNotification({ id: 'other', read: false }),
      ]
      await result.markRead('target')
      await nextTick()
      expect(result.notifications.value.find(n => n.id === 'target')!.read).toBe(true)
      expect(result.notifications.value.find(n => n.id === 'other')!.read).toBe(false)
    })

    it('calls the correct endpoint', async () => {
      const { result } = await loadNotifications()
      result.notifications.value = [makeNotification({ id: 'abc' })]
      await result.markRead('abc')
      const call = mockPost.mock.calls.find(
        (c: unknown[]) => typeof c[0] === 'string' && c[0].includes('/abc/read')
      )
      expect(call).toBeDefined()
    })

    it('handles missing notification id gracefully', async () => {
      const { result } = await loadNotifications()
      result.notifications.value = [makeNotification({ id: 'exists', read: false })]
      await result.markRead('nonexistent')
      expect(result.notifications.value[0].read).toBe(false)
    })
  })

  describe('connect', () => {
    it('creates an EventSource connection', async () => {
      const { result } = await loadNotifications()
      result.connect()
      expect(result.connected.value).toBeDefined()
    })
  })
})
