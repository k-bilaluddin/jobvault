<script setup lang="ts">
import { ref, onMounted } from 'vue'
import AppHeader from '@/components/layout/AppHeader.vue'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import { api } from '@/api'

interface Settings {
  github: {
    owner: string
    repository: string
    branch: string
    cvFileName: string
    coverLetterFileName: string
  }
  telegram: {
    chatId: string
  }
}

const loading = ref(true)
const saving = ref(false)
const saved = ref(false)
const error = ref('')

const form = ref<Settings>({
  github: {
    owner: 'k-bilaluddin',
    repository: 'job-applications-vault',
    branch: 'master',
    cvFileName: 'KhawajaBilal_Uddin_CV',
    coverLetterFileName: 'KhawajaBilal_Uddin_CoverLetter',
  },
  telegram: {
    chatId: '',
  },
})

async function loadSettings() {
  loading.value = true
  try {
    const { data } = await api.get('/api/settings')
    form.value = data
  } catch {
    error.value = 'Failed to load settings'
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  saved.value = false
  error.value = ''
  try {
    const { data } = await api.put('/api/settings', form.value)
    form.value = data
    saved.value = true
    setTimeout(() => saved.value = false, 3000)
  } catch {
    error.value = 'Failed to save settings'
  } finally {
    saving.value = false
  }
}

onMounted(loadSettings)
</script>

<template>
  <div class="flex h-screen bg-surface overflow-hidden">
    <AppSidebar />
    <div class="flex-1 flex flex-col overflow-hidden">
      <AppHeader title="Settings" />

      <div class="flex-1 overflow-y-auto p-6">
        <div v-if="loading" class="text-sm text-text-muted text-center py-12">Loading settings...</div>

        <div v-else class="max-w-2xl space-y-6">
          <!-- GitHub -->
          <div class="bg-surface-raised border border-border rounded-xl p-5">
            <div class="flex items-center gap-2 mb-4">
              <div class="w-8 h-8 rounded-lg bg-slate-500/10 flex items-center justify-center">
                <svg class="w-4 h-4 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4"/>
                </svg>
              </div>
              <div>
                <p class="text-sm font-semibold text-text-primary">GitHub</p>
                <p class="text-xs text-text-muted">Vault repository configuration</p>
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-text-muted uppercase tracking-wider mb-1">Owner</label>
                <input v-model="form.github.owner"
                  class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary outline-none focus:border-accent"/>
              </div>
              <div>
                <label class="block text-xs font-medium text-text-muted uppercase tracking-wider mb-1">Repository</label>
                <input v-model="form.github.repository"
                  class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary outline-none focus:border-accent"/>
              </div>
              <div>
                <label class="block text-xs font-medium text-text-muted uppercase tracking-wider mb-1">Branch</label>
                <input v-model="form.github.branch"
                  class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary outline-none focus:border-accent"/>
              </div>
              <div class="col-span-2 border-t border-border pt-4 mt-1">
                <p class="text-xs text-text-muted mb-3">File naming used when generating CV and cover letter documents</p>
              </div>
              <div>
                <label class="block text-xs font-medium text-text-muted uppercase tracking-wider mb-1">CV File Name</label>
                <input v-model="form.github.cvFileName"
                  class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary outline-none focus:border-accent"/>
                <p class="text-[11px] text-text-muted mt-1">.docx and .pdf extensions added automatically</p>
              </div>
              <div>
                <label class="block text-xs font-medium text-text-muted uppercase tracking-wider mb-1">Cover Letter File Name</label>
                <input v-model="form.github.coverLetterFileName"
                  class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary outline-none focus:border-accent"/>
                <p class="text-[11px] text-text-muted mt-1">.docx and .pdf extensions added automatically</p>
              </div>
            </div>
          </div>

          <!-- Telegram -->
          <div class="bg-surface-raised border border-border rounded-xl p-5">
            <div class="flex items-center gap-2 mb-4">
              <div class="w-8 h-8 rounded-lg bg-blue-500/10 flex items-center justify-center">
                <svg class="w-4 h-4 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"/>
                </svg>
              </div>
              <div>
                <p class="text-sm font-semibold text-text-primary">Telegram</p>
                <p class="text-xs text-text-muted">Notification channel configuration</p>
              </div>
            </div>

            <div>
              <label class="block text-xs font-medium text-text-muted uppercase tracking-wider mb-1">Chat ID</label>
              <input v-model="form.telegram.chatId"
                class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary outline-none focus:border-accent"
                placeholder="e.g. -1001234567890"/>
              <p class="text-[11px] text-text-muted mt-1">Bot token is configured via environment variable for security</p>
            </div>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-3">
            <button @click="save" :disabled="saving"
              class="flex items-center gap-1.5 px-5 py-2 text-sm font-medium text-white bg-accent rounded-lg hover:bg-accent/90 disabled:opacity-50 transition-colors">
              <svg v-if="saving" class="w-3.5 h-3.5 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"/>
              </svg>
              {{ saving ? 'Saving...' : 'Save Settings' }}
            </button>

            <transition enter-active-class="transition-opacity duration-200" enter-from-class="opacity-0" leave-active-class="transition-opacity duration-200" leave-to-class="opacity-0">
              <span v-if="saved" class="text-xs text-emerald-400 font-medium">Settings saved successfully</span>
            </transition>

            <span v-if="error" class="text-xs text-red-400">{{ error }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
