import { ref } from 'vue'

const deferredPrompt = ref<any>(null)
const canInstall = ref(false)
const installed = ref(false)

// Capture the install prompt
window.addEventListener('beforeinstallprompt', (e) => {
  e.preventDefault()
  deferredPrompt.value = e
  canInstall.value = true
})

window.addEventListener('appinstalled', () => {
  canInstall.value = false
  installed.value = true
  deferredPrompt.value = null
})

export function usePWA() {
  async function install() {
    if (!deferredPrompt.value) return
    deferredPrompt.value.prompt()
    const { outcome } = await deferredPrompt.value.userChoice
    if (outcome === 'accepted') {
      canInstall.value = false
      deferredPrompt.value = null
    }
  }

  return { canInstall, installed, install }
}
