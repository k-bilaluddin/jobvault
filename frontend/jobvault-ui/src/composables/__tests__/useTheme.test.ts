import { describe, it, expect, vi, beforeEach } from 'vitest'
import { nextTick } from 'vue'
import { withSetup } from './helpers'

describe('useTheme', () => {
  beforeEach(() => {
    vi.resetModules()
    localStorage.clear()
    document.documentElement.classList.remove('light', 'theme-transition')
  })

  async function loadTheme() {
    const mod = await import('../useTheme')
    return withSetup(() => mod.useTheme())
  }

  it('defaults to dark theme', async () => {
    const { result } = await loadTheme()
    expect(result.isDark.value).toBe(true)
    expect(result.theme.value).toBe('dark')
  })

  it('toggle switches from dark to light', async () => {
    const { result } = await loadTheme()
    result.toggle()
    await nextTick()
    expect(result.theme.value).toBe('light')
    expect(result.isDark.value).toBe(false)
  })

  it('toggle switches back to dark', async () => {
    const { result } = await loadTheme()
    result.toggle()
    await nextTick()
    result.toggle()
    await nextTick()
    expect(result.theme.value).toBe('dark')
    expect(result.isDark.value).toBe(true)
  })

  it('setTheme sets the theme directly', async () => {
    const { result } = await loadTheme()
    result.setTheme('light')
    await nextTick()
    expect(result.theme.value).toBe('light')
    expect(result.isDark.value).toBe(false)
  })

  it('persists theme to localStorage on change', async () => {
    const { result } = await loadTheme()
    result.toggle()
    await nextTick()
    expect(localStorage.getItem('jobvault-theme')).toBe('light')
  })

  it('applies light class to documentElement', async () => {
    const { result } = await loadTheme()
    result.setTheme('light')
    await nextTick()
    expect(document.documentElement.classList.contains('light')).toBe(true)
  })

  it('removes light class when switching to dark', async () => {
    const { result } = await loadTheme()
    result.setTheme('light')
    await nextTick()
    result.setTheme('dark')
    await nextTick()
    expect(document.documentElement.classList.contains('light')).toBe(false)
  })

  it('reads stored light theme on init', async () => {
    localStorage.setItem('jobvault-theme', 'light')
    const { result } = await loadTheme()
    expect(result.theme.value).toBe('light')
  })
})
