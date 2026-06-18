import { vi } from 'vitest'

vi.stubGlobal('EventSource', class MockEventSource {
  url: string
  onopen: (() => void) | null = null
  onmessage: ((event: MessageEvent) => void) | null = null
  onerror: (() => void) | null = null
  readyState = 0
  constructor(url: string) {
    this.url = url
  }
  close() {
    this.readyState = 2
  }
  addEventListener() {}
  removeEventListener() {}
  dispatchEvent() { return false }
})
