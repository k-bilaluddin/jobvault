<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useTheme } from '@/composables/useTheme'
import { usePWA } from '@/composables/usePWA'
import { useNotifications } from '@/composables/useNotifications'

defineProps<{ title: string }>()

const { isDark, toggle } = useTheme()
const router = useRouter()
const { canInstall, install } = usePWA()
const { notifications, unreadCount, connected, markAllRead, markRead } = useNotifications()

const dropdownOpen = ref(false)
const dropdownRef = ref<HTMLElement | null>(null)

// Show at most 20 newest notifications
const recentNotifications = computed(() => notifications.value.slice(0, 20))

function toggleDropdown() {
  dropdownOpen.value = !dropdownOpen.value
}

function closeDropdown() {
  dropdownOpen.value = false
}

function handleClickOutside(event: MouseEvent) {
  if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
    closeDropdown()
  }
}

async function handleMarkAllRead() {
  await markAllRead()
}

async function handleNotificationClick(id: string, companySlug: string | null) {
  await markRead(id)
  closeDropdown()
  if (companySlug) {
    router.push(`/company/${companySlug}`)
  }
}

function relativeTime(isoDate: string): string {
  const diff = Date.now() - new Date(isoDate).getTime()
  const mins = Math.floor(diff / 60_000)
  if (mins < 1) return 'just now'
  if (mins < 60) return `${mins} min${mins === 1 ? '' : 's'} ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs} hr${hrs === 1 ? '' : 's'} ago`
  const days = Math.floor(hrs / 24)
  return `${days} day${days === 1 ? '' : 's'} ago`
}

function notificationIcon(type: string): string {
  switch (type) {
    case 'new_application': return '+'
    case 'stage_changed':   return '↕'
    case 'score_computed':  return '%'
    case 'sync_completed':  return '↺'
    default:                return '•'
  }
}

function notificationIconClass(type: string): string {
  switch (type) {
    case 'new_application': return 'bg-emerald-500/15 text-emerald-400'
    case 'stage_changed':   return 'bg-amber-500/15 text-amber-400'
    case 'score_computed':  return 'bg-blue-500/15 text-blue-400'
    case 'sync_completed':  return 'bg-violet-500/15 text-violet-400'
    default:                return 'bg-surface-overlay text-text-muted'
  }
}

function logout() {
  localStorage.removeItem('jv_auth')
  router.push('/login')
}

onMounted(() => document.addEventListener('mousedown', handleClickOutside))
onUnmounted(() => document.removeEventListener('mousedown', handleClickOutside))
</script>

<template>
  <header class="flex items-center justify-between px-6 py-4 border-b border-border bg-surface-raised flex-shrink-0">
    <h1 class="text-base font-semibold text-text-primary">{{ title }}</h1>

    <div class="flex items-center gap-3">
      <!-- Connection status -->
      <div class="flex items-center gap-1.5 text-xs text-text-muted">
        <span
          class="w-2 h-2 rounded-full"
          :class="connected ? 'bg-emerald-500 animate-pulse' : 'bg-red-500'"
        />
        <span class="text-text-secondary font-medium">
          {{ connected ? 'Connected' : 'Disconnected' }}
        </span>
      </div>

      <!-- PWA Install -->
      <button v-if="canInstall" @click="install"
        class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium bg-accent/15 text-accent border border-accent/30 rounded-lg hover:bg-accent/25 transition-colors">
        <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
        </svg>
        Install App
      </button>

      <!-- Notifications -->
      <div ref="dropdownRef" class="relative">
        <button
          @click="toggleDropdown"
          class="relative p-2 rounded-lg hover:bg-surface-overlay text-text-muted hover:text-text-primary transition-colors"
        >
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
          </svg>
          <span
            v-if="unreadCount > 0"
            class="absolute top-1 right-1 w-4 h-4 bg-accent text-white text-[9px] font-bold rounded-full flex items-center justify-center"
          >
            {{ unreadCount > 99 ? '99+' : unreadCount }}
          </span>
        </button>

        <!-- Dropdown -->
        <div
          v-if="dropdownOpen"
          class="absolute right-0 top-full mt-2 w-80 bg-surface-raised border border-border rounded-xl shadow-xl z-50 overflow-hidden"
        >
          <!-- Header -->
          <div class="flex items-center justify-between px-4 py-3 border-b border-border">
            <span class="text-sm font-semibold text-text-primary">Notifications</span>
            <button
              v-if="unreadCount > 0"
              @click="handleMarkAllRead"
              class="text-xs text-accent hover:text-accent/80 font-medium transition-colors"
            >
              Mark all read
            </button>
          </div>

          <!-- List -->
          <div class="max-h-96 overflow-y-auto">
            <template v-if="recentNotifications.length > 0">
              <button
                v-for="n in recentNotifications"
                :key="n.id"
                @click="handleNotificationClick(n.id, n.companySlug)"
                class="w-full flex items-start gap-3 px-4 py-3 text-left hover:bg-surface-overlay transition-colors border-b border-border/50 last:border-0"
                :class="!n.read ? 'border-l-2 border-l-accent bg-accent/5' : ''"
              >
                <!-- Type icon -->
                <span
                  class="flex-shrink-0 w-7 h-7 rounded-full flex items-center justify-center text-sm font-bold mt-0.5"
                  :class="notificationIconClass(n.type)"
                >
                  {{ notificationIcon(n.type) }}
                </span>

                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-text-primary leading-tight">{{ n.title }}</p>
                  <p class="text-xs text-text-muted truncate mt-0.5">{{ n.body }}</p>
                  <p class="text-[10px] text-text-muted/70 mt-1">{{ relativeTime(n.occurredAt) }}</p>
                </div>

                <!-- Unread dot -->
                <span v-if="!n.read" class="flex-shrink-0 w-2 h-2 rounded-full bg-accent mt-2" />
              </button>
            </template>

            <!-- Empty state -->
            <div v-else class="flex flex-col items-center justify-center py-10 text-text-muted">
              <svg class="w-8 h-8 mb-2 opacity-40" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
              </svg>
              <p class="text-xs">No notifications yet</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Theme -->
      <button @click="toggle" class="p-2 rounded-lg hover:bg-surface-overlay text-text-muted hover:text-text-primary transition-colors">
        <svg v-if="isDark" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"/>
        </svg>
        <svg v-else class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"/>
        </svg>
      </button>

      <!-- Logout -->
      <button @click="logout" class="p-2 rounded-lg hover:bg-red-500/10 text-text-muted hover:text-red-400 transition-colors">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
        </svg>
      </button>
    </div>
  </header>
</template>
