<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { api } from '@/api'
import type { ApplicationContent, RolePayload } from '@/types'

const props = defineProps<{
  companyName: string
  jobUrl?: string
  isRegenerating: boolean
}>()

const emit = defineEmits<{
  regenerated: []
}>()

const loading = ref(false)
const saving = ref(false)
const content = ref<ApplicationContent | null>(null)
const editorTab = ref<'cv' | 'cover-letter'>('cv')
const error = ref('')

async function loadContent() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get(`/api/applications/${encodeURIComponent(props.companyName)}/content`)
    content.value = data
  } catch {
    error.value = 'Failed to load content'
  } finally {
    loading.value = false
  }
}

watch(() => props.companyName, loadContent, { immediate: true })

function addBullet(role: RolePayload) {
  role.bullets.push('')
}

function removeBullet(role: RolePayload, idx: number) {
  role.bullets.splice(idx, 1)
}

function addSkill() {
  content.value?.skills.push({ label: '', value: '' })
}

function removeSkill(idx: number) {
  content.value?.skills.splice(idx, 1)
}

function addParagraph() {
  content.value?.coverLetterParagraphs.push('')
}

function removeParagraph(idx: number) {
  content.value?.coverLetterParagraphs.splice(idx, 1)
}

async function regenerate() {
  if (!content.value) return
  saving.value = true
  error.value = ''
  try {
    await api.post(`/api/applications/${encodeURIComponent(props.companyName)}/regenerate`, {
      headline: content.value.headline,
      summary: content.value.summary,
      skills: content.value.skills,
      roles: content.value.roles,
      recipient: content.value.recipient,
      coverLetterParagraphs: content.value.coverLetterParagraphs,
      strengths: content.value.strengths,
      gaps: content.value.gaps,
    })
    emit('regenerated')
  } catch {
    error.value = 'Failed to start regeneration'
  } finally {
    saving.value = false
  }
}

// Non-null assertion for template — guarded by v-else which only renders when content is set
const c = computed(() => content.value!)

const copyToast = ref('')

async function reviewWithChatGPT() {
  if (!content.value) return
  const coverLetter = content.value.coverLetterParagraphs.join('\n\n')
  await navigator.clipboard.writeText(coverLetter)
  const prompt = encodeURIComponent(
    `I'm applying for this role: ${props.jobUrl || '(see cover letter)'} — I'll paste my cover letter below. Please review it: tone, alignment with the JD, weak spots, and what to improve.`
  )
  window.open(`https://chatgpt.com/?q=${prompt}`, '_blank')
  copyToast.value = 'Cover letter copied — paste it into ChatGPT (Ctrl+V)'
  setTimeout(() => { copyToast.value = '' }, 4000)
}

const ROLE_LABELS: Record<string, string> = {
  calvergy: 'Calvergy',
  senior_baris: 'Senior — Baris',
  developer_baris: 'Developer — Baris',
  junior_baris: 'Junior — Baris',
}
</script>

<template>
  <!-- Regenerating overlay -->
  <div v-if="isRegenerating" class="bg-surface-raised border border-indigo-500/30 rounded-xl p-8 text-center">
    <div class="inline-flex items-center justify-center w-12 h-12 rounded-full bg-indigo-500/15 mb-4">
      <svg class="w-6 h-6 text-indigo-400 animate-spin" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"/>
      </svg>
    </div>
    <p class="text-sm font-medium text-text-primary mb-1">Regenerating documents</p>
    <p class="text-xs text-text-muted">Your CV and cover letter are being regenerated. This usually takes a minute.</p>
  </div>

  <!-- Loading -->
  <div v-else-if="loading" class="bg-surface-raised border border-border rounded-xl p-8 text-center">
    <p class="text-sm text-text-muted">Loading content…</p>
  </div>

  <!-- No content -->
  <div v-else-if="!content || !content.headline" class="bg-surface-raised border border-border rounded-xl p-6 text-center">
    <p class="text-sm text-text-muted">No editable content available for this application.</p>
    <p class="text-xs text-text-muted mt-1">Content is only available for applications ingested through the async pipeline.</p>
  </div>

  <!-- Toast -->
  <teleport to="body">
    <div v-if="copyToast"
      class="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 bg-surface-raised border border-border rounded-lg px-4 py-2.5 text-xs text-text-primary shadow-lg flex items-center gap-2">
      <svg class="w-4 h-4 text-green-400 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
      </svg>
      {{ copyToast }}
    </div>
  </teleport>

  <!-- Editor -->
  <div v-else class="space-y-4">
    <!-- Tab switch -->
    <div class="flex items-center justify-between">
      <p class="text-xs font-semibold text-text-muted uppercase tracking-wider">Edit content</p>
      <div class="inline-flex bg-surface-overlay rounded-lg p-0.5 gap-0.5">
        <button @click="editorTab = 'cv'"
          :class="['px-3 py-1.5 text-xs font-medium rounded-md transition-colors', editorTab === 'cv' ? 'bg-accent/15 text-accent' : 'text-text-muted hover:text-text-secondary']">
          CV
        </button>
        <button @click="editorTab = 'cover-letter'"
          :class="['px-3 py-1.5 text-xs font-medium rounded-md transition-colors', editorTab === 'cover-letter' ? 'bg-accent/15 text-accent' : 'text-text-muted hover:text-text-secondary']">
          Cover Letter
        </button>
      </div>
    </div>

    <!-- CV editor -->
    <div v-if="editorTab === 'cv'" class="bg-surface-raised border border-border rounded-xl p-5 space-y-5">
      <!-- Headline -->
      <div>
        <label class="block text-xs font-medium text-text-muted uppercase tracking-wider mb-1.5">Headline</label>
        <input v-model="c.headline"
          class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary outline-none focus:border-accent"/>
      </div>

      <!-- Summary -->
      <div>
        <label class="block text-xs font-medium text-text-muted uppercase tracking-wider mb-1.5">Summary</label>
        <textarea v-model="c.summary" rows="3"
          class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary outline-none focus:border-accent resize-vertical"/>
      </div>

      <!-- Skills -->
      <div>
        <div class="flex items-center justify-between mb-2">
          <label class="text-xs font-medium text-text-muted uppercase tracking-wider">Skills</label>
          <button @click="addSkill" class="text-xs text-accent hover:text-accent/80 flex items-center gap-1">
            <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
            Add
          </button>
        </div>
        <div class="grid grid-cols-2 gap-2">
          <div v-for="(skill, idx) in c.skills" :key="idx"
            class="flex items-center gap-2 bg-surface-overlay border border-border rounded-lg px-3 py-1.5 group">
            <input v-model="skill.label" placeholder="Label"
              class="w-20 bg-transparent text-xs text-text-muted outline-none flex-shrink-0"/>
            <input v-model="skill.value" placeholder="Value"
              class="flex-1 bg-transparent text-xs text-text-primary outline-none"/>
            <button @click="removeSkill(idx)" class="text-text-muted hover:text-red-400 opacity-0 group-hover:opacity-100 transition-opacity">
              <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>
          </div>
        </div>
      </div>

      <!-- Role bullets -->
      <div>
        <label class="block text-xs font-medium text-text-muted uppercase tracking-wider mb-2">Role Bullets</label>
        <div v-for="role in c.roles" :key="role.id" class="mb-4">
          <div class="flex items-center gap-2 mb-2">
            <span class="text-xs font-medium text-accent bg-accent/10 px-2 py-0.5 rounded">{{ role.id }}</span>
            <span class="text-xs text-text-muted">{{ ROLE_LABELS[role.id] || role.id }}</span>
          </div>
          <div class="space-y-1.5 pl-3 border-l-2 border-border">
            <div v-for="(_bullet, bIdx) in role.bullets" :key="bIdx" class="flex items-start gap-2 group">
              <span class="w-1.5 h-1.5 rounded-full bg-accent mt-2 flex-shrink-0"/>
              <textarea v-model="role.bullets[bIdx]" rows="2"
                class="flex-1 bg-surface-overlay border border-border rounded-lg px-3 py-1.5 text-xs text-text-secondary outline-none focus:border-accent resize-vertical"/>
              <button @click="removeBullet(role, bIdx)" class="text-text-muted hover:text-red-400 opacity-0 group-hover:opacity-100 transition-opacity mt-1.5">
                <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
              </button>
            </div>
            <button @click="addBullet(role)" class="text-xs text-accent hover:text-accent/80 flex items-center gap-1 ml-3 mt-1">
              <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              Add bullet
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Cover letter editor -->
    <div v-else class="bg-surface-raised border border-border rounded-xl p-5 space-y-4">
      <!-- Recipient -->
      <div>
        <label class="block text-xs font-medium text-text-muted uppercase tracking-wider mb-1.5">Recipient</label>
        <input v-model="c.recipient"
          class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary outline-none focus:border-accent"
          placeholder="Dear Hiring Manager,"/>
      </div>

      <!-- Paragraphs -->
      <div>
        <div class="flex items-center justify-between mb-2">
          <label class="text-xs font-medium text-text-muted uppercase tracking-wider">Paragraphs</label>
          <div class="flex items-center gap-3">
            <button @click="reviewWithChatGPT" class="text-xs text-emerald-400 hover:text-emerald-300 flex items-center gap-1">
              <svg class="w-3 h-3" viewBox="0 0 24 24" fill="currentColor">
                <path d="M22.282 9.821a5.985 5.985 0 0 0-.516-4.91 6.046 6.046 0 0 0-6.51-2.9A6.065 6.065 0 0 0 4.981 4.18a5.985 5.985 0 0 0-3.998 2.9 6.046 6.046 0 0 0 .743 7.097 5.98 5.98 0 0 0 .51 4.911 6.051 6.051 0 0 0 6.515 2.9A5.985 5.985 0 0 0 13.26 24a6.056 6.056 0 0 0 5.772-4.206 5.99 5.99 0 0 0 3.997-2.9 6.056 6.056 0 0 0-.747-7.073zM13.26 22.43a4.476 4.476 0 0 1-2.876-1.04l.141-.081 4.779-2.758a.795.795 0 0 0 .392-.681v-6.737l2.02 1.168a.071.071 0 0 1 .038.052v5.583a4.504 4.504 0 0 1-4.494 4.494zM3.6 18.304a4.47 4.47 0 0 1-.535-3.014l.142.085 4.783 2.759a.771.771 0 0 0 .78 0l5.843-3.369v2.332a.08.08 0 0 1-.033.062L9.74 19.95a4.5 4.5 0 0 1-6.14-1.646zM2.34 7.896a4.485 4.485 0 0 1 2.366-1.973V11.6a.766.766 0 0 0 .388.676l5.815 3.355-2.02 1.168a.076.076 0 0 1-.071 0l-4.83-2.786A4.504 4.504 0 0 1 2.34 7.896zm16.597 3.855l-5.843-3.369 2.02-1.168a.076.076 0 0 1 .071 0l4.83 2.786a4.494 4.494 0 0 1-.676 8.105v-5.678a.79.79 0 0 0-.402-.676zm2.01-3.023l-.141-.085-4.774-2.782a.776.776 0 0 0-.785 0L9.409 9.23V6.897a.066.066 0 0 1 .028-.061l4.83-2.787a4.5 4.5 0 0 1 6.68 4.66zm-12.64 4.135l-2.02-1.164a.08.08 0 0 1-.038-.057V6.075a4.5 4.5 0 0 1 7.375-3.453l-.142.08-4.778 2.758a.795.795 0 0 0-.393.681zm1.097-2.365l2.602-1.5 2.607 1.5v2.999l-2.597 1.5-2.607-1.5z"/>
              </svg>
              Review with ChatGPT
            </button>
            <button @click="addParagraph" class="text-xs text-accent hover:text-accent/80 flex items-center gap-1">
              <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              Add
            </button>
          </div>
        </div>
        <div class="space-y-3">
          <div v-for="(_para, idx) in c.coverLetterParagraphs" :key="idx" class="relative group">
            <div class="absolute -left-2 top-2.5 w-4 h-4 rounded-full bg-surface-raised border border-border flex items-center justify-center">
              <span class="text-[9px] text-text-muted font-mono">{{ idx + 1 }}</span>
            </div>
            <div class="flex items-start gap-2 pl-4">
              <textarea v-model="c.coverLetterParagraphs[idx]" rows="3"
                class="flex-1 bg-surface-overlay border border-border rounded-lg px-3 py-2 text-xs text-text-secondary outline-none focus:border-accent resize-vertical leading-relaxed"/>
              <button @click="removeParagraph(idx)" class="text-text-muted hover:text-red-400 opacity-0 group-hover:opacity-100 transition-opacity mt-2">
                <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Error -->
    <p v-if="error" class="text-xs text-red-400">{{ error }}</p>

    <!-- Regenerate bar -->
    <div class="flex items-center justify-between bg-surface-overlay border border-border rounded-xl px-4 py-3">
      <p class="text-xs text-text-muted">
        <svg class="w-3.5 h-3.5 inline-block mr-1 -mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
        Changes will regenerate DOCX &amp; PDF, then sync to your vault
      </p>
      <button @click="regenerate" :disabled="saving"
        class="flex items-center gap-1.5 px-4 py-2 text-xs font-medium text-white bg-accent rounded-lg hover:bg-accent/90 disabled:opacity-50 transition-colors">
        <svg class="w-3.5 h-3.5" :class="saving && 'animate-spin'" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
        </svg>
        {{ saving ? 'Starting…' : 'Regenerate & sync' }}
      </button>
    </div>
  </div>
</template>
