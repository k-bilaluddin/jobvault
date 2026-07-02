import { ref } from 'vue'

const sidebarOpen = ref(false)

export function useSidebar() {
  function toggleSidebar() {
    sidebarOpen.value = !sidebarOpen.value
  }

  function openSidebar() {
    sidebarOpen.value = true
  }

  function closeSidebar() {
    sidebarOpen.value = false
  }

  return { sidebarOpen, toggleSidebar, openSidebar, closeSidebar }
}
