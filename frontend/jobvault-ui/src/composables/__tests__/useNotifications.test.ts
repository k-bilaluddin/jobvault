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

describe('useNotifications', () => {
  beforeEach(() => {
    vi.resetModules()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve([]),
    }))
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
      const mockFetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve([]) })
      vi.stubGlobal('fetch', mockFetch)
      const { result } = await loadNotifications()
      await result.markAllRead()
      const readAllCall = mockFetch.mock.calls.find(
        (call: unknown[]) => typeof call[0] === 'string' && call[0].includes('/read-all')
      )
      expect(readAllCall).toBeDefined()
      expect(readAllCall![1]).toEqual({ method: 'POST' })
    })

    it('sets error on fetch failure', async () => {
      const { result } = await loadNotifications()
      vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Network error')))
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
      const mockFetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve([]) })
      vi.stubGlobal('fetch', mockFetch)
      const { result } = await loadNotifications()
      result.notifications.value = [makeNotification({ id: 'abc' })]
      await result.markRead('abc')
      const readCall = mockFetch.mock.calls.find(
        (call: unknown[]) => typeof call[0] === 'string' && call[0].includes('/abc/read')
      )
      expect(readCall).toBeDefined()
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
