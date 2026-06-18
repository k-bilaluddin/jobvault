import { describe, it, expect, vi, beforeEach } from 'vitest'

describe('usePWA', () => {
  beforeEach(() => {
    vi.resetModules()
  })

  async function loadPWA() {
    const mod = await import('../usePWA')
    return mod.usePWA()
  }

  it('returns canInstall as false initially', async () => {
    const { canInstall } = await loadPWA()
    expect(canInstall.value).toBe(false)
  })

  it('returns installed as false initially', async () => {
    const { installed } = await loadPWA()
    expect(installed.value).toBe(false)
  })

  it('install does nothing when no deferred prompt', async () => {
    const { install, canInstall } = await loadPWA()
    await install()
    expect(canInstall.value).toBe(false)
  })

  it('sets canInstall after beforeinstallprompt event', async () => {
    // Import first so event listeners are registered
    const { canInstall } = await loadPWA()

    const event = new Event('beforeinstallprompt') as any
    event.preventDefault = vi.fn()
    window.dispatchEvent(event)

    expect(canInstall.value).toBe(true)
  })

  it('sets installed after appinstalled event', async () => {
    const { installed, canInstall } = await loadPWA()

    window.dispatchEvent(new Event('appinstalled'))

    expect(installed.value).toBe(true)
    expect(canInstall.value).toBe(false)
  })
})
