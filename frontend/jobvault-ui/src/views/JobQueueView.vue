<script setup lang="ts">
import { ref } from 'vue'
import AppHeader from '@/components/layout/AppHeader.vue'
import { useJobQueue } from '@/composables/useJobQueue'

const { jobs, loading, filterStatus, counts, addJob, updateJob, deleteJob, cleanup } = useJobQueue()

const newUrl = ref('')
const newPrompt = ref('')
const showPrompt = ref(false)
const adding = ref(false)
const editingId = ref<string | null>(null)
const editUrl = ref('')
const toast = ref('')
const toastError = ref(false)

function showToast(msg: string, error = false) {
  toast.value = msg
  toastError.value = error
  setTimeout(() => { toast.value = '' }, 3000)
}

async function handleAdd() {
  const url = newUrl.value.trim()
  if (!url) return
  adding.value = true
  try {
    await addJob(url, newPrompt.value.trim() || undefined)
    newUrl.value = ''
    newPrompt.value = ''
    showPrompt.value = false
    showToast('Job URL queued')
  } catch {
    showToast('Failed to add URL', true)
  } finally {
    adding.value = false
  }
}

function startEdit(id: string, url: string) {
  editingId.value = id
  editUrl.value = url
}

async function saveEdit(id: string) {
  try {
    await updateJob(id, editUrl.value)
    editingId.value = null
    showToast('Updated')
  } catch {
    showToast('Update failed', true)
  }
}

async function handleDelete(id: string) {
  try {
    await deleteJob(id)
    showToast('Deleted')
  } catch {
    showToast('Delete failed', true)
  }
}

async function handleCleanup(status: string) {
  try {
    const n = await cleanup(status)
    showToast(`Cleaned up ${n} ${status} job${n !== 1 ? 's' : ''}`)
  } catch {
    showToast('Cleanup failed', true)
  }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
</script>

<template>
  <div class="flex-1 flex flex-col h-full overflow-hidden">
    <AppHeader title="Job Queue" />

    <div class="flex-1 overflow-y-auto p-4 md:p-6 space-y-6">

      <!-- Add URL -->
      <form @submit.prevent="handleAdd" class="space-y-2">
        <div class="flex gap-3">
          <input v-model="newUrl" type="url" placeholder="Paste job URL..."
            class="flex-1 bg-surface-overlay border border-border text-text-primary text-sm rounded-lg px-4 py-2.5 outline-none focus:border-accent transition-colors placeholder:text-text-muted" />
          <button type="button" @click="showPrompt = !showPrompt"
            class="px-3 py-2.5 text-xs font-medium rounded-lg border transition-colors"
            :class="showPrompt ? 'bg-orange-500/15 text-orange-400 border-orange-500/30' : 'text-text-muted border-border hover:text-text-secondary hover:border-accent/30'"
            title="Add guidance prompt for Claude agent">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z"/></svg>
          </button>
          <button type="submit" :disabled="adding || !newUrl.trim()"
            class="px-5 py-2.5 bg-accent text-white text-sm font-medium rounded-lg hover:bg-accent/90 disabled:opacity-40 disabled:cursor-not-allowed transition-all">
            {{ adding ? 'Adding...' : 'Add to Queue' }}
          </button>
        </div>
        <div v-if="showPrompt">
          <textarea v-model="newPrompt" rows="2" placeholder="Optional guidance for Claude — e.g. &quot;Emphasize cloud migration experience&quot;"
            class="w-full bg-surface-overlay border border-border rounded-lg px-4 py-2 text-sm text-text-primary placeholder:text-text-muted outline-none focus:border-orange-400/50 resize-none transition-colors" />
        </div>
      </form>

      <!-- Filter tabs + cleanup -->
      <div class="flex items-center justify-between flex-wrap gap-2">
        <div class="flex gap-1 flex-wrap">
          <button v-for="s in (['all', 'pending', 'done', 'failed'] as const)" :key="s"
            @click="filterStatus = s"
            class="px-3 py-1.5 text-xs font-medium rounded-lg transition-colors capitalize"
            :class="filterStatus === s
              ? 'bg-accent/15 text-accent'
              : 'text-text-muted hover:text-text-secondary hover:bg-surface-overlay'">
            {{ s }} <span class="ml-1 opacity-60">{{ counts[s] }}</span>
          </button>
        </div>
        <div class="flex gap-2">
          <button v-if="counts.done > 0" @click="handleCleanup('done')"
            class="px-3 py-1.5 text-xs text-text-muted hover:text-emerald-400 hover:bg-emerald-500/10 rounded-lg transition-colors">
            Clear done ({{ counts.done }})
          </button>
          <button v-if="counts.failed > 0" @click="handleCleanup('failed')"
            class="px-3 py-1.5 text-xs text-text-muted hover:text-red-400 hover:bg-red-500/10 rounded-lg transition-colors">
            Clear failed ({{ counts.failed }})
          </button>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="text-center py-12 text-text-muted text-sm">Loading...</div>

      <!-- Empty state -->
      <div v-else-if="jobs.length === 0" class="text-center py-16">
        <svg class="w-12 h-12 mx-auto text-text-muted/30 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"/>
        </svg>
        <p class="text-text-muted text-sm">No jobs in queue</p>
        <p class="text-text-muted/60 text-xs mt-1">Paste a job URL above to get started</p>
      </div>

      <!-- Jobs table -->
      <div v-else class="border border-border rounded-xl overflow-x-auto">
        <table class="w-full min-w-[560px]">
          <thead>
            <tr class="bg-surface-overlay text-text-muted text-xs uppercase tracking-wider">
              <th class="text-left px-4 py-3 font-medium">URL</th>
              <th class="text-left px-4 py-3 font-medium w-24">Status</th>
              <th class="text-left px-4 py-3 font-medium w-44">Created</th>
              <th class="text-right px-4 py-3 font-medium w-28">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-border">
            <tr v-for="job in jobs" :key="job.jobId" class="hover:bg-surface-overlay/50 transition-colors">
              <td class="px-4 py-3">
                <template v-if="editingId === job.jobId">
                  <div class="flex gap-2">
                    <input v-model="editUrl" type="url"
                      class="flex-1 bg-surface-overlay border border-accent text-text-primary text-sm rounded px-3 py-1 outline-none"
                      @keyup.enter="saveEdit(job.jobId)" @keyup.escape="editingId = null" />
                    <button @click="saveEdit(job.jobId)" class="text-xs text-accent hover:text-accent/80">Save</button>
                    <button @click="editingId = null" class="text-xs text-text-muted hover:text-text-secondary">Cancel</button>
                  </div>
                </template>
                <template v-else>
                  <a :href="job.url" target="_blank" rel="noopener"
                    class="text-sm text-accent hover:underline truncate block max-w-lg">
                    {{ job.url }}
                  </a>
                </template>
              </td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center gap-1.5 text-xs font-medium px-2 py-0.5 rounded-full"
                  :class="{
                    'bg-amber-500/10 text-amber-400': job.status === 'pending',
                    'bg-emerald-500/10 text-emerald-400': job.status === 'done',
                    'bg-red-500/10 text-red-400': job.status === 'failed',
                  }">
                  <span class="w-1.5 h-1.5 rounded-full"
                    :class="{
                      'bg-amber-400': job.status === 'pending',
                      'bg-emerald-400': job.status === 'done',
                      'bg-red-400': job.status === 'failed',
                    }" />
                  {{ job.status }}
                </span>
              </td>
              <td class="px-4 py-3 text-xs text-text-muted">{{ formatDate(job.createdAt) }}</td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1">
                  <button v-if="job.status === 'pending'" @click="startEdit(job.jobId, job.url)"
                    class="p-1.5 text-text-muted hover:text-text-primary hover:bg-surface-overlay rounded transition-colors"
                    title="Edit">
                    <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
                    </svg>
                  </button>
                  <button @click="handleDelete(job.jobId)"
                    class="p-1.5 text-text-muted hover:text-red-400 hover:bg-red-500/10 rounded transition-colors"
                    title="Delete">
                    <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Toast -->
    <Transition name="toast">
      <div v-if="toast"
        class="fixed bottom-5 right-5 z-50 flex items-center gap-3 px-4 py-3 rounded-xl shadow-lg border text-sm font-medium max-w-xs"
        :class="toastError ? 'bg-red-950 border-red-700 text-red-200' : 'bg-emerald-950 border-emerald-700 text-emerald-200'">
        {{ toast }}
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.toast-enter-active, .toast-leave-active { transition: all 0.3s ease; }
.toast-enter-from, .toast-leave-to { opacity: 0; transform: translateY(8px); }
</style>
