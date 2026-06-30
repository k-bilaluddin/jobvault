<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppHeader from '@/components/layout/AppHeader.vue'
import CompanyAvatar from '@/components/common/CompanyAvatar.vue'
import RecommendBadge from '@/components/common/RecommendBadge.vue'
import MatchRing from '@/components/common/MatchRing.vue'
import { useCompanies } from '@/composables/useCompanies'
import { STAGE_COLORS } from '@/utils/score'
import { PIPELINE_STAGES, NOTE_CATEGORIES } from '@/types'
import type { ApplicationStage, ApplicationNote, NoteCategory } from '@/types'
import { api, API_BASE } from '@/api'
import ContentEditor from '@/components/company/ContentEditor.vue'

const route  = useRoute()
const router = useRouter()
const { getByName, loading } = useCompanies()

const companyName = computed(() => decodeURIComponent(route.params.name as string))
const company     = computed(() => getByName(companyName.value))

const TABS = ['Analysis', 'Strategy', 'Details', 'My Notes', 'Files', 'Interviews', 'Journey'] as const
type Tab = typeof TABS[number]
const activeTab = ref<Tab>('Analysis')

// ── Stage dropdown ────────────────────────────────────────────
const currentStage = ref<ApplicationStage>('Applied')
const stageUpdating = ref(false)
const stageError = ref('')

watch(() => company.value?.stage, (s) => {
  if (s) currentStage.value = s as ApplicationStage
}, { immediate: true })

async function updateStage(newStage: ApplicationStage) {
  if (newStage === currentStage.value) return
  stageUpdating.value = true
  stageError.value = ''
  try {
    await api.post(`/api/applications/${encodeURIComponent(companyName.value)}/stage`, { stage: newStage })
    currentStage.value = newStage
    // Update local cache
    const c = company.value
    if (c) c.stage = newStage
  } catch (e) {
    stageError.value = 'Failed to update stage'
  } finally {
    stageUpdating.value = false
  }
}

// ── Re-analyze (re-queue for Claude agent) ──────────────────
const showReAnalyze = ref(false)
const reAnalyzePrompt = ref('')
const reAnalyzeLoading = ref(false)
const reAnalyzeSuccess = ref(false)
const reAnalyzeError = ref('')

async function submitReAnalyze() {
  reAnalyzeLoading.value = true
  reAnalyzeError.value = ''
  try {
    await api.post(`/api/applications/${encodeURIComponent(companyName.value)}/re-queue`, {
      prompt: reAnalyzePrompt.value.trim() || null
    })
    reAnalyzeSuccess.value = true
    if (company.value) {
      company.value.status = 'Queued'
      company.value.stage = 'Queued' as ApplicationStage
    }
    setTimeout(() => {
      showReAnalyze.value = false
      reAnalyzeSuccess.value = false
      reAnalyzePrompt.value = ''
    }, 2000)
  } catch {
    reAnalyzeError.value = 'Failed to queue for re-analysis'
  } finally {
    reAnalyzeLoading.value = false
  }
}

// ── Compatibility report HTML (from Flask) ────────────────────
const reportHtml   = ref('')
const reportLoading = ref(false)
const reportLoaded  = ref(false)

async function loadReport() {
  if (reportLoaded.value) return
  reportLoading.value = true
  try {
    const { data } = await api.get(`/api/applications/${encodeURIComponent(companyName.value)}/report`)
    reportHtml.value = data.html ?? ''
    reportLoaded.value = true
  } catch {
    reportHtml.value = '<p class="text-red-400 text-sm">Failed to load report.</p>'
    reportLoaded.value = true
  } finally {
    reportLoading.value = false
  }
}

// ── Tailoring notes HTML ─────────────────────────────────────
const notesHtml    = ref('')
const notesLoading  = ref(false)
const notesLoaded   = ref(false)

async function loadNotes() {
  if (notesLoaded.value) return
  notesLoading.value = true
  try {
    const { data } = await api.get(`/api/applications/${encodeURIComponent(companyName.value)}/notes`)
    notesHtml.value = data.html ?? ''
    notesLoaded.value = true
  } catch {
    notesHtml.value = '<p class="text-red-400 text-sm">Failed to load notes.</p>'
    notesLoaded.value = true
  } finally {
    notesLoading.value = false
  }
}

// Load on tab switch
watch(activeTab, (tab) => {
  if (tab === 'Analysis') loadReport()
  if (tab === 'Strategy') loadNotes()
})

// Reset and reload when company changes
watch(companyName, () => {
  reportLoaded.value = false
  reportHtml.value = ''
  notesLoaded.value = false
  notesHtml.value = ''
  noteInit.value = false
  notesInit.value = false
  interviewsInit.value = false
  if (activeTab.value === 'Analysis') loadReport()
  if (activeTab.value === 'Strategy') loadNotes()
})

// Load report on mount if it's the default tab
watch(() => company.value, (c) => {
  if (c && activeTab.value === 'Analysis') loadReport()
}, { immediate: true })

// ── Personal notes ────────────────────────────────────────────
const noteText    = ref('')
const noteEditing = ref(false)
const noteInit    = ref(false)
const noteSaving  = ref(false)
const noteSaved   = ref(false)

watch(() => company.value, (c) => {
  if (c && !noteInit.value) {
    noteText.value = c.personal_notes ?? ''
    noteInit.value = true
  }
}, { immediate: true })

async function saveNote() {
  noteSaving.value = true
  try {
    await api.post(`/api/applications/${encodeURIComponent(companyName.value)}/personal-notes`, { notes: noteText.value })
    if (company.value) company.value.personal_notes = noteText.value
    noteEditing.value = false
    noteSaved.value = true
    setTimeout(() => noteSaved.value = false, 2000)
  } finally {
    noteSaving.value = false
  }
}

// ── PDF viewer ────────────────────────────────────────────────
const pdfViewer  = ref<'cv' | 'letter' | null>(null)
const showEditor = ref(false)
const pdfUrl = computed(() => {
  if (!pdfViewer.value) return ''
  const token = localStorage.getItem('jv_token') ?? ''
  return `${API_BASE}/api/applications/${encodeURIComponent(companyName.value)}/pdf/${pdfViewer.value}?token=${encodeURIComponent(token)}`
})

// ── Salary formatter ─────────────────────────────────────────
const salaryDisplay = computed(() => {
  const s = company.value?.salary
  if (!s) return null
  if (s.offered)    return { label: 'Offered',    value: formatSal(s.offered)    }
  if (s.discussed)  return { label: 'Discussed',  value: formatSal(s.discussed)  }
  if (s.advertised) return { label: 'Advertised', value: formatSal(s.advertised) }
  if (s.target)     return { label: 'Target',     value: formatSal(s.target)     }
  return null
})

function formatSal(v: string) {
  if (!v) return ''
  // Reject values that look like dates (e.g. 21052026) or are unrealistically large
  if (v.includes('-')) {
    const parts = v.split('-').map(n => parseInt(n))
    if (parts.some(n => isNaN(n) || n > 500000)) return '—'
    const [lo, hi] = parts.map(n => n >= 1000 ? Math.round(n / 1000) + 'k' : String(n))
    return `€${lo} – €${hi}`
  }
  const n = parseInt(v)
  if (isNaN(n) || n > 500000 || n < 100) return '—'
  return n >= 1000 ? `€${Math.round(n / 1000)}k` : `€${n}`
}


function copyToClipboard(text: string) {
  if (!text) return
  const el = document.createElement('textarea')
  el.value = text
  document.body.appendChild(el)
  el.select()
  document.execCommand('copy')
  document.body.removeChild(el)
}


// ── Interviews ────────────────────────────────────────────────
const localInterviews = ref<typeof company.value extends null ? never[] : NonNullable<typeof company.value>['interviews']>([])
const interviewsInit  = ref(false)

watch(() => company.value?.interviews, (ivs) => {
  if (ivs && !interviewsInit.value) {
    localInterviews.value = JSON.parse(JSON.stringify(ivs))
    interviewsInit.value = true
  }
}, { immediate: true })

const newInterview = ref({
  date:    new Date().toISOString().split('T')[0],
  type:    'HR',
  notes:   '',
  outcome: 'Pending',
})
const addingInterview = ref(false)
const ivSaving        = ref(false)
const ivError         = ref('')

async function saveInterview() {
  const resolvedType = newInterview.value.type === 'Other'
    ? (customInterviewType.value.trim() || 'Other')
    : newInterview.value.type
  ivSaving.value = true
  ivError.value  = ''
  try {
    const payload = { ...newInterview.value, type: resolvedType }
    const { data } = await api.post(`/api/applications/${encodeURIComponent(companyName.value)}/interviews`, payload)
    if (data.ok) {
      localInterviews.value = data.interviews
      if (company.value) company.value.interviews = data.interviews
      addingInterview.value = false
      newInterview.value = { date: new Date().toISOString().split('T')[0], type: 'HR', notes: '', outcome: 'Pending' }
      customInterviewType.value = ''
    }
  } catch { ivError.value = 'Failed to save' }
  finally  { ivSaving.value = false }
}

async function updateInterviewOutcome(idx: number, outcome: string) {
  try {
    const { data } = await api.put(`/api/applications/${encodeURIComponent(companyName.value)}/interviews/${idx}`, { outcome })
    if (data.ok) {
      localInterviews.value = data.interviews
      if (company.value) company.value.interviews = data.interviews
    }
  } catch { /* ignore */ }
}

async function deleteInterview(idx: number) {
  try {
    const { data } = await api.delete(`/api/applications/${encodeURIComponent(companyName.value)}/interviews?idx=${idx}`)
    if (data.ok) {
      localInterviews.value = localInterviews.value.filter((_, i) => i !== idx)
      if (company.value) company.value.interviews = localInterviews.value
    }
  } catch { /* ignore */ }
}

// ── Notes ────────────────────────────────────────────────────
const localNotes = ref<ApplicationNote[]>([])
const notesInit  = ref(false)

watch(() => company.value?.notes, (ns) => {
  if (ns && !notesInit.value) {
    localNotes.value = JSON.parse(JSON.stringify(ns))
    notesInit.value = true
  }
}, { immediate: true })

const addingNote     = ref(false)
const noteSavingNew  = ref(false)
const noteError      = ref('')
const noteFilterCat  = ref<NoteCategory | 'All'>('All')

const newNote = ref({
  category: 'General' as NoteCategory,
  content: '',
  pinned: false,
  stage: '',
})

const filteredNotes = computed(() => {
  let list = localNotes.value
  if (noteFilterCat.value !== 'All')
    list = list.filter(n => n.category === noteFilterCat.value)
  return [...list].sort((a, b) => {
    if (a.pinned !== b.pinned) return a.pinned ? -1 : 1
    return new Date(b.created_at).getTime() - new Date(a.created_at).getTime()
  })
})

async function saveNewNote() {
  if (!newNote.value.content.trim()) { noteError.value = 'Please add content'; return }
  noteSavingNew.value = true
  noteError.value = ''
  try {
    const { data } = await api.post(`/api/applications/${encodeURIComponent(companyName.value)}/notes`, newNote.value)
    if (data.ok) {
      localNotes.value = data.notes
      if (company.value) company.value.notes = data.notes
      addingNote.value = false
      newNote.value = { category: 'General', content: '', pinned: false, stage: '' }
    }
  } catch { noteError.value = 'Failed to save' }
  finally  { noteSavingNew.value = false }
}

async function togglePin(note: ApplicationNote) {
  try {
    const { data } = await api.put(`/api/applications/${encodeURIComponent(companyName.value)}/notes/${note.id}`, { pinned: !note.pinned })
    if (data.ok) {
      localNotes.value = data.notes
      if (company.value) company.value.notes = data.notes
    }
  } catch { /* ignore */ }
}

async function deleteNote(noteId: number) {
  try {
    const { data } = await api.delete(`/api/applications/${encodeURIComponent(companyName.value)}/notes/${noteId}`)
    if (data.ok) {
      localNotes.value = localNotes.value.filter(n => n.id !== noteId)
      if (company.value) company.value.notes = localNotes.value
    }
  } catch { /* ignore */ }
}

const NOTE_STYLE: Record<string, { icon: string; color: string; bg: string; border: string }> = {
  Application: { icon: '📄', color: 'text-blue-400',    bg: 'bg-blue-500/5',    border: 'border-blue-500/20'    },
  Interview:   { icon: '🎤', color: 'text-violet-400',  bg: 'bg-violet-500/5',  border: 'border-violet-500/20'  },
  'Follow-up': { icon: '📬', color: 'text-emerald-400', bg: 'bg-emerald-500/5', border: 'border-emerald-500/20' },
  Rejection:   { icon: '📋', color: 'text-red-400',     bg: 'bg-red-500/5',     border: 'border-red-500/20'     },
  General:     { icon: '💡', color: 'text-indigo-400',  bg: 'bg-indigo-500/5',  border: 'border-indigo-500/20'  },
}

// Pipeline stages for timeline
const TIMELINE_STAGES = ['Ready to Apply', 'Applied', 'Interview', 'Offer'] as const

const timelineStages = computed(() => {
  const stages = [...TIMELINE_STAGES] as string[]
  if (currentStage.value === 'Rejected') stages.push('Rejected')
  return stages
})

function notesForStage(stage: string) {
  return localNotes.value
    .filter(n => n.stage === stage)
    .sort((a, b) => {
      if (a.pinned !== b.pinned) return a.pinned ? -1 : 1
      return new Date(a.created_at).getTime() - new Date(b.created_at).getTime()
    })
}

function isStageCompleted(stage: string) {
  if (stage === 'Rejected') return currentStage.value === 'Rejected'
  if (stage === 'Offer') return currentStage.value === 'Offer'
  const order = ['Ready to Apply', 'Applied', 'Interview', 'Offer']
  const current = currentStage.value === 'Rejected'
    ? lastReachedStage.value
    : currentStage.value
  return order.indexOf(current) >= order.indexOf(stage)
}

const lastReachedStage = computed(() => {
  if (company.value?.interviews?.length) return 'Interview'
  if (company.value?.applied) return 'Applied'
  return 'Ready to Apply'
})

function formatNoteDate(iso: string) {
  const d = new Date(iso)
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

const daysInPipeline = computed(() => {
  const created = company.value?.applied_date || company.value?.synced_at
  if (!created) return null
  const start = new Date(created)
  const now = new Date()
  return Math.max(0, Math.floor((now.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)))
})

const noteCountsByCategory = computed(() => {
  const counts: Record<string, number> = {}
  for (const cat of NOTE_CATEGORIES) counts[cat] = 0
  for (const n of localNotes.value) counts[n.category] = (counts[n.category] ?? 0) + 1
  return counts
})

const INTERVIEW_TYPES = ['HR', 'Technical', 'Phone', 'Onsite', 'Final', 'Other']
const customInterviewType = ref('')
const INTERVIEW_OUTCOMES = ['Pending', 'Pass', 'Fail']

const TYPE_STYLE: Record<string, { dot: string; badge: string }> = {
  HR:        { dot: 'bg-blue-500',    badge: 'bg-blue-500/15 text-blue-400 border-blue-500/30'       },
  Technical: { dot: 'bg-violet-500',  badge: 'bg-violet-500/15 text-violet-400 border-violet-500/30' },
  Phone:     { dot: 'bg-sky-500',     badge: 'bg-sky-500/15 text-sky-400 border-sky-500/30'          },
  Onsite:    { dot: 'bg-amber-500',   badge: 'bg-amber-500/15 text-amber-400 border-amber-500/30'    },
  Final:     { dot: 'bg-emerald-500', badge: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30' },
}

const OUTCOME_STYLE: Record<string, { selected: string; unselected: string }> = {
  Pending: { selected: 'bg-amber-500 text-white',   unselected: 'bg-surface-overlay text-amber-400 border border-amber-500/30 hover:bg-amber-500/15' },
  Pass:    { selected: 'bg-emerald-500 text-white',  unselected: 'bg-surface-overlay text-emerald-400 border border-emerald-500/30 hover:bg-emerald-500/15' },
  Fail:    { selected: 'bg-red-500 text-white',      unselected: 'bg-surface-overlay text-red-400 border border-red-500/30 hover:bg-red-500/15' },
}

</script>

<template>
  <div class="flex flex-col h-full">
    <AppHeader :title="company?.name ?? 'Company'" />

    <!-- PDF viewer modal -->
    <div v-if="pdfViewer" class="fixed inset-0 z-50 bg-black/80 flex flex-col">
      <div class="flex items-center justify-between px-4 py-3 bg-surface-raised border-b border-border">
        <p class="text-sm font-medium text-text-primary">
          {{ pdfViewer === 'cv' ? 'CV' : 'Cover Letter' }} — {{ company?.name }}
        </p>
        <button @click="pdfViewer = null" class="text-text-muted hover:text-text-primary p-1">
          <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
        </button>
      </div>
      <iframe :src="pdfUrl" class="flex-1 w-full border-0" />
    </div>

    <div v-if="loading" class="flex-1 flex items-center justify-center">
      <div class="animate-spin w-8 h-8 border-2 border-accent border-t-transparent rounded-full"/>
    </div>
    <div v-else-if="!company" class="flex-1 flex flex-col items-center justify-center gap-3">
      <p class="text-4xl">🔍</p>
      <p class="text-text-secondary">Company not found</p>
      <button @click="router.push('/applications')" class="text-accent text-sm hover:underline">← Back</button>
    </div>

    <div v-else class="flex-1 overflow-y-auto flex flex-col">

      <!-- Header bar -->
      <div class="px-6 py-4 border-b border-border flex items-center gap-4 flex-wrap flex-shrink-0">
        <CompanyAvatar :name="company.name" size="lg"/>
        <div class="flex-1 min-w-0">
          <h2 class="text-lg font-bold text-text-primary">{{ company.name }}</h2>
          <div class="flex items-center gap-2 mt-0.5 flex-wrap text-xs text-text-muted">
            <span v-if="company.applied_date">Applied {{ company.applied_date }}</span>
            <span v-if="company.recruiter?.name"> · {{ company.recruiter.name }}</span>
            <span v-if="company.source" class="px-1.5 py-0.5 rounded bg-surface-overlay text-text-muted">{{ company.source }}</span>
          </div>
        </div>

        <!-- Stage dropdown — wired to Flask -->
        <div class="relative">
          <select
            :value="currentStage"
            :disabled="stageUpdating"
            @change="updateStage(($event.target as HTMLSelectElement).value as ApplicationStage)"
            class="appearance-none bg-surface-raised border border-border text-text-primary text-sm rounded-lg px-3 py-2 pr-8 outline-none focus:border-accent cursor-pointer disabled:opacity-50 transition-colors"
            :class="stageUpdating ? 'animate-pulse' : ''">
            <option v-for="s in PIPELINE_STAGES" :key="s" :value="s">{{ s }}</option>
          </select>
          <svg class="w-4 h-4 text-text-muted absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
          </svg>
        </div>
        <p v-if="stageError" class="text-xs text-red-400">{{ stageError }}</p>

        <RecommendBadge :recommend="company.recommend"/>

        <a v-if="company.job_url" :href="company.job_url" target="_blank"
          class="flex items-center gap-2 px-3 py-2 bg-blue-500/15 text-blue-400 text-sm font-medium rounded-lg hover:bg-blue-500/25 border border-blue-500/30 transition-colors">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"/></svg>
          Job Posting
        </a>
        <span v-else class="text-xs text-text-muted">No job URL</span>

        <button v-if="company.job_url" @click="showReAnalyze = !showReAnalyze"
          class="flex items-center gap-2 px-3 py-2 text-sm font-medium rounded-lg border transition-colors"
          :class="showReAnalyze ? 'bg-orange-500/20 text-orange-400 border-orange-500/40' : 'bg-orange-500/10 text-orange-400 border-orange-500/20 hover:bg-orange-500/20'">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/></svg>
          Re-analyze
        </button>
      </div>

      <!-- Re-analyze prompt panel -->
      <Transition enter-active-class="transition-all duration-150 ease-out" enter-from-class="opacity-0 -translate-y-2" enter-to-class="opacity-100 translate-y-0"
        leave-active-class="transition-all duration-100 ease-in" leave-from-class="opacity-100 translate-y-0" leave-to-class="opacity-0 -translate-y-2">
        <div v-if="showReAnalyze" class="px-6 py-3 border-b border-border bg-orange-500/5 flex-shrink-0">
          <div class="flex items-start gap-3">
            <div class="flex-1">
              <p class="text-xs font-semibold text-orange-400 mb-1.5">Re-analyze with Claude Agent</p>
              <textarea v-model="reAnalyzePrompt" rows="2" placeholder="Optional guidance — e.g. &quot;Emphasize cloud migration experience&quot; or &quot;Cover letter should be more formal&quot;"
                class="w-full bg-surface-raised border border-border rounded-lg px-3 py-2 text-sm text-text-primary placeholder:text-text-muted outline-none focus:border-orange-400/50 resize-none transition-colors"/>
            </div>
            <div class="flex flex-col gap-1.5 pt-5">
              <button @click="submitReAnalyze" :disabled="reAnalyzeLoading || reAnalyzeSuccess"
                class="px-4 py-2 text-xs font-medium rounded-lg transition-colors"
                :class="reAnalyzeSuccess ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30' : 'bg-orange-500/20 text-orange-400 border border-orange-500/30 hover:bg-orange-500/30 disabled:opacity-50'">
                {{ reAnalyzeSuccess ? 'Queued!' : reAnalyzeLoading ? 'Queuing...' : 'Queue' }}
              </button>
              <button @click="showReAnalyze = false" class="px-4 py-1.5 text-xs text-text-muted hover:text-text-primary transition-colors">
                Cancel
              </button>
            </div>
          </div>
          <p v-if="reAnalyzeError" class="text-xs text-red-400 mt-1">{{ reAnalyzeError }}</p>
          <p class="text-[10px] text-text-muted mt-1">The Claude agent routine will pick this up on its next hourly run and re-generate everything from scratch.</p>
        </div>
      </Transition>

      <!-- Tabs -->
      <div class="px-6 border-b border-border flex gap-0 overflow-x-auto flex-shrink-0">
        <button v-for="tab in TABS" :key="tab" @click="activeTab = tab"
          class="px-4 py-3 text-sm font-medium whitespace-nowrap border-b-2 transition-colors flex-shrink-0"
          :class="activeTab === tab ? 'border-accent text-accent' : 'border-transparent text-text-muted hover:text-text-primary'">
          {{ tab }}
          <span v-if="tab === 'Interviews' && (company.interviews?.length ?? 0) > 0"
            class="ml-1 text-[10px] bg-violet-500/20 text-violet-400 px-1 rounded">{{ company.interviews.length }}</span>
          <span v-if="tab === 'My Notes' && localNotes.length > 0"
            class="ml-1 text-[10px] bg-indigo-500/20 text-indigo-400 px-1 rounded">{{ localNotes.length }}</span>
        </button>
      </div>

      <!-- Tab content -->
      <div class="flex-1 overflow-y-auto p-6">

        <!-- ── COMPATIBILITY REPORT ── -->
        <div v-if="activeTab === 'Analysis'" class="grid grid-cols-3 gap-6">
          <div class="col-span-1 space-y-4">
            <div class="bg-surface-raised border border-border rounded-xl p-5 flex flex-col items-center">
              <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Match Score</p>
              <MatchRing :pct="company.match_pct" :recommend="company.recommend" :size="128"/>
              <RecommendBadge :recommend="company.recommend" class="mt-3"/>
            </div>
            <div class="bg-surface-raised border border-border rounded-xl p-4 space-y-2.5">
              <p class="text-xs font-semibold text-text-muted uppercase tracking-wider">Quick Info</p>
              <div v-if="salaryDisplay" class="flex justify-between text-xs">
                <span class="text-text-muted">{{ salaryDisplay.label }}</span>
                <span class="text-text-primary font-medium">{{ salaryDisplay.value }}</span>
              </div>
              <div v-if="company.recruiter?.name" class="flex justify-between text-xs">
                <span class="text-text-muted">Contact</span>
                <span class="text-text-primary">{{ company.recruiter.name }}</span>
              </div>
              <div v-if="company.recruiter?.email" class="flex justify-between text-xs">
                <span class="text-text-muted">Email</span>
                <a :href="`mailto:${company.recruiter.email}`" class="text-accent hover:underline truncate max-w-[120px]">{{ company.recruiter.email }}</a>
              </div>
              <div v-if="company.follow_up_date" class="flex justify-between text-xs">
                <span class="text-text-muted">Follow-up</span>
                <span class="text-amber-400 font-medium">{{ company.follow_up_date }}</span>
              </div>
              <div v-if="(company.interviews?.length ?? 0) > 0" class="flex justify-between text-xs">
                <span class="text-text-muted">Interviews</span>
                <span class="text-violet-400 font-medium">{{ company.interviews.length }} round{{ company.interviews.length > 1 ? 's' : '' }}</span>
              </div>
            </div>
          </div>

          <!-- Report HTML rendered from Flask -->
          <div class="col-span-2">
            <div class="bg-surface-raised border border-border rounded-xl p-5 min-h-[300px]">
              <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Compatibility Report</p>
              <div v-if="reportLoading" class="flex items-center gap-2 text-text-muted text-sm">
                <div class="w-4 h-4 border-2 border-accent border-t-transparent rounded-full animate-spin flex-shrink-0"/>
                Loading report...
              </div>
              <!-- Rendered markdown from Flask -->
              <div v-else-if="reportHtml"
                class="prose-report text-text-secondary text-sm leading-relaxed"
                v-html="reportHtml"/>
            </div>
          </div>
        </div>

        <!-- ── TAILORING NOTES ── -->
        <div v-else-if="activeTab === 'Strategy'" class="max-w-3xl">
          <div class="bg-surface-raised border border-border rounded-xl p-5 min-h-[300px]">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Tailoring Notes</p>
            <div v-if="notesLoading" class="flex items-center gap-2 text-text-muted text-sm">
              <div class="w-4 h-4 border-2 border-accent border-t-transparent rounded-full animate-spin flex-shrink-0"/>
              Loading notes...
            </div>
            <div v-else-if="notesHtml"
              class="prose-report text-text-secondary text-sm leading-relaxed"
              v-html="notesHtml"/>
          </div>
        </div>

        <!-- ── DETAILS ── -->
        <div v-else-if="activeTab === 'Details'" class="max-w-3xl space-y-5">

          <!-- Application overview cards -->
          <div class="grid grid-cols-3 gap-4">
            <!-- Stage -->
            <div class="bg-surface-raised border border-border rounded-xl p-4">
              <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-2">Stage</p>
              <div class="flex items-center gap-2">
                <span :class="['w-2 h-2 rounded-full flex-shrink-0', STAGE_COLORS[company.stage as ApplicationStage]?.dot ?? 'bg-slate-500']"/>
                <span :class="['text-sm font-semibold', STAGE_COLORS[company.stage as ApplicationStage]?.text ?? 'text-slate-400']">{{ company.stage }}</span>
              </div>
            </div>
            <!-- Applied date -->
            <div class="bg-surface-raised border border-border rounded-xl p-4">
              <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-2">Applied</p>
              <p class="text-sm font-semibold text-text-primary">{{ company.applied_date || '—' }}</p>
              <p class="text-[10px] text-text-muted mt-0.5">{{ company.applied ? 'Submitted' : 'Not yet applied' }}</p>
            </div>
            <!-- Source -->
            <div class="bg-surface-raised border border-border rounded-xl p-4">
              <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-2">Source</p>
              <div class="flex items-center gap-1.5">
                <svg class="w-3.5 h-3.5 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"/>
                </svg>
                <span class="text-sm font-semibold text-text-primary">{{ company.source || 'Direct' }}</span>
              </div>
            </div>
          </div>

          <!-- Follow-up -->
          <div v-if="company.follow_up_date"
            :class="['rounded-xl p-4 border flex items-center gap-3',
              company.follow_up_date <= new Date().toISOString().split('T')[0]
                ? 'bg-amber-500/10 border-amber-500/30'
                : 'bg-surface-raised border-border']">
            <span class="text-xl">⏰</span>
            <div>
              <p class="text-xs font-semibold"
                :class="company.follow_up_date <= new Date().toISOString().split('T')[0] ? 'text-amber-400' : 'text-text-primary'">
                Follow-up {{ company.follow_up_date <= new Date().toISOString().split('T')[0] ? 'overdue' : 'scheduled' }}
              </p>
              <p class="text-sm font-bold"
                :class="company.follow_up_date <= new Date().toISOString().split('T')[0] ? 'text-amber-400' : 'text-text-secondary'">
                {{ company.follow_up_date }}
              </p>
            </div>
          </div>

          <!-- Recruiter card -->
          <div v-if="company.recruiter?.name || company.recruiter?.email || company.recruiter?.linkedin"
            class="bg-surface-raised border border-border rounded-xl p-5">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Recruiter / Contact</p>
            <div class="flex items-start gap-4">
              <!-- Avatar -->
              <div class="w-10 h-10 rounded-full bg-accent/20 border border-accent/30 flex items-center justify-center flex-shrink-0">
                <svg class="w-5 h-5 text-accent" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
                </svg>
              </div>
              <div class="flex-1 space-y-2">
                <!-- Name — strip email if duplicated in name field -->
                <p v-if="company.recruiter.name" class="text-sm font-semibold text-text-primary">
                  {{ company.recruiter.name.split('—')[0].split('(')[0].trim() }}
                </p>
                <!-- Email -->
                <a v-if="company.recruiter.email"
                  :href="`mailto:${company.recruiter.email}`"
                  class="flex items-center gap-2 text-xs text-accent hover:underline w-fit">
                  <svg class="w-3.5 h-3.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
                  </svg>
                  {{ company.recruiter.email }}
                </a>
                <!-- LinkedIn -->
                <a v-if="company.recruiter.linkedin"
                  :href="company.recruiter.linkedin" target="_blank"
                  class="flex items-center gap-2 text-xs text-blue-400 hover:underline w-fit">
                  <svg class="w-3.5 h-3.5 flex-shrink-0" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 01-2.063-2.065 2.064 2.064 0 112.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>
                  </svg>
                  LinkedIn Profile
                </a>
                <!-- Extra notes from recruiter name field (after —) -->
                <p v-if="company.recruiter.name?.includes('—') || company.recruiter.name?.includes('(')"
                  class="text-[10px] text-text-muted leading-relaxed">
                  {{ company.recruiter.name }}
                </p>
              </div>
              <!-- Quick email copy -->
              <button v-if="company.recruiter.email"
                @click="copyToClipboard(company.recruiter?.email ?? '')"
                class="flex items-center gap-1.5 px-3 py-1.5 text-xs bg-surface-overlay hover:bg-border rounded-lg text-text-muted hover:text-text-primary transition-colors flex-shrink-0">
                <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"/></svg>
                Copy
              </button>
            </div>
          </div>

          <!-- Salary card -->
          <div class="bg-surface-raised border border-border rounded-xl p-5">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Salary</p>
            <div class="grid grid-cols-2 gap-4">
              <div v-for="(val, key) in company.salary" :key="key"
                class="p-3 rounded-lg bg-surface-overlay border border-border">
                <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-1.5">{{ key }}</p>
                <p class="text-base font-bold"
                  :class="val ? 'text-emerald-400' : 'text-text-muted'">
                  {{ val ? formatSal(val) : '—' }}
                </p>
              </div>
            </div>
          </div>

          <!-- Vault files status -->
          <div class="bg-surface-raised border border-border rounded-xl p-5">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Vault Files</p>
            <div class="grid grid-cols-2 gap-3">
              <div v-for="f in [
                { label: 'Compatibility Report', present: company.has_report,     icon: '📄' },
                { label: 'Tailoring Notes',      present: company.has_notes,      icon: '📝' },
                { label: 'CV PDF',               present: company.has_cv_pdf,     icon: '📋' },
                { label: 'Cover Letter PDF',      present: company.has_letter_pdf, icon: '✉️'  },
              ]" :key="f.label"
                class="flex items-center gap-3 p-3 rounded-lg border"
                :class="f.present ? 'bg-emerald-500/5 border-emerald-500/20' : 'bg-surface-overlay border-border opacity-50'">
                <span class="text-base">{{ f.icon }}</span>
                <div>
                  <p class="text-xs font-medium" :class="f.present ? 'text-text-primary' : 'text-text-muted'">{{ f.label }}</p>
                  <p class="text-[10px]" :class="f.present ? 'text-emerald-400' : 'text-text-muted'">
                    {{ f.present ? '✓ Available' : '✗ Not found' }}
                  </p>
                </div>
              </div>
            </div>
          </div>

        </div>

        <!-- ── MY NOTES ── -->
        <div v-else-if="activeTab === 'My Notes'" class="max-w-2xl space-y-4">

          <!-- Filter + Add bar -->
          <div class="flex items-center gap-3 mb-4">
            <div class="flex gap-1.5 flex-1 overflow-x-auto min-w-0">
              <button @click="noteFilterCat = 'All'"
                :class="['px-2.5 py-1 text-xs font-medium rounded-full border transition-colors whitespace-nowrap',
                  noteFilterCat === 'All' ? 'bg-accent/15 text-accent border-accent/30' : 'bg-surface-overlay text-text-muted border-border hover:border-accent/40']">
                All ({{ localNotes.length }})
              </button>
              <button v-for="cat in NOTE_CATEGORIES" :key="cat" @click="noteFilterCat = cat"
                :class="['px-2.5 py-1 text-xs font-medium rounded-full border transition-colors whitespace-nowrap',
                  noteFilterCat === cat
                    ? `${NOTE_STYLE[cat].bg} ${NOTE_STYLE[cat].color} ${NOTE_STYLE[cat].border}`
                    : 'bg-surface-overlay text-text-muted border-border hover:border-accent/40']">
                {{ NOTE_STYLE[cat].icon }} {{ cat }} ({{ localNotes.filter(n => n.category === cat).length }})
              </button>
            </div>
            <button @click="addingNote = !addingNote"
              :class="['flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg transition-colors flex-shrink-0',
                addingNote ? 'bg-surface-overlay text-text-muted' : 'bg-accent/15 text-accent hover:bg-accent/25 border border-accent/30']">
              <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              {{ addingNote ? 'Cancel' : 'Add Note' }}
            </button>
          </div>

          <!-- Add note form -->
          <Transition enter-active-class="transition-all duration-200 ease-out" enter-from-class="opacity-0 -translate-y-2" enter-to-class="opacity-100 translate-y-0">
            <div v-if="addingNote" class="bg-surface-raised border border-accent/40 rounded-xl p-5 space-y-4">
              <p class="text-sm font-semibold text-text-primary">New Note</p>
              <div>
                <label class="block text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-1.5">Stage</label>
                <div class="flex flex-wrap gap-1.5">
                  <button v-for="s in TIMELINE_STAGES" :key="s" @click="newNote.stage = s"
                    :class="['px-2.5 py-1 text-xs font-medium rounded-full border transition-colors',
                      newNote.stage === s
                        ? `${STAGE_COLORS[s as ApplicationStage]?.bg ?? 'bg-accent/15'} ${STAGE_COLORS[s as ApplicationStage]?.text ?? 'text-accent'} border-transparent`
                        : 'bg-surface-overlay text-text-muted border-border hover:border-accent/40']">
                    {{ s }}
                  </button>
                </div>
              </div>
              <div>
                <label class="block text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-1.5">Category</label>
                <div class="flex flex-wrap gap-1.5">
                  <button v-for="cat in NOTE_CATEGORIES" :key="cat" @click="newNote.category = cat"
                    :class="['px-2.5 py-1 text-xs font-medium rounded-full border transition-colors',
                      newNote.category === cat
                        ? `${NOTE_STYLE[cat].bg} ${NOTE_STYLE[cat].color} ${NOTE_STYLE[cat].border}`
                        : 'bg-surface-overlay text-text-muted border-border hover:border-accent/40']">
                    {{ NOTE_STYLE[cat].icon }} {{ cat }}
                  </button>
                </div>
              </div>
              <div>
                <label class="block text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-1.5">Content</label>
                <textarea v-model="newNote.content" rows="3"
                  placeholder="What happened, what's important to remember..."
                  class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2.5 text-sm text-text-primary placeholder:text-text-muted outline-none focus:border-accent transition-colors resize-none"/>
              </div>
              <div class="flex items-center gap-3">
                <label class="flex items-center gap-2 cursor-pointer">
                  <input type="checkbox" v-model="newNote.pinned" class="accent-accent w-3.5 h-3.5"/>
                  <span class="text-xs text-text-muted">Pin this note</span>
                </label>
              </div>
              <div class="flex items-center justify-between gap-3 pt-1">
                <p v-if="noteError" class="text-xs text-red-400">{{ noteError }}</p>
                <div v-else/>
                <div class="flex gap-2">
                  <button @click="addingNote = false; noteError = ''" class="px-4 py-2 text-xs text-text-muted hover:text-text-primary bg-surface-overlay rounded-lg transition-colors">Cancel</button>
                  <button @click="saveNewNote" :disabled="noteSavingNew"
                    class="flex items-center gap-2 px-4 py-2 text-xs font-semibold bg-accent text-white rounded-lg hover:bg-accent/90 disabled:opacity-50 transition-colors">
                    <svg v-if="noteSavingNew" class="w-3.5 h-3.5 animate-spin" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
                    {{ noteSavingNew ? 'Saving…' : 'Save Note' }}
                  </button>
                </div>
              </div>
            </div>
          </Transition>

          <!-- Notes list -->
          <div v-if="filteredNotes.length" class="space-y-2">
            <div v-for="note in filteredNotes" :key="note.id"
              :class="['flex gap-3 p-3.5 rounded-xl border group transition-colors',
                NOTE_STYLE[note.category]?.bg ?? 'bg-surface-raised',
                NOTE_STYLE[note.category]?.border ?? 'border-border']">
              <span class="text-sm flex-shrink-0 mt-0.5">{{ NOTE_STYLE[note.category]?.icon ?? '💡' }}</span>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1.5 flex-wrap">
                  <span :class="['text-[9px] font-semibold uppercase tracking-wider px-1.5 py-0.5 rounded',
                    `${NOTE_STYLE[note.category]?.bg} ${NOTE_STYLE[note.category]?.color}`]">
                    {{ note.category }}
                  </span>
                  <span class="text-[9px] text-text-muted">{{ formatNoteDate(note.created_at) }}</span>
                  <span class="text-[9px] text-text-muted px-1.5 py-0.5 bg-surface-overlay rounded">{{ note.stage }}</span>
                  <span v-if="note.pinned" class="text-[9px] px-1.5 py-0.5 bg-amber-500/10 text-amber-400 rounded font-medium">📌 Pinned</span>
                </div>
                <p class="text-sm text-text-secondary leading-relaxed whitespace-pre-wrap">{{ note.content }}</p>
              </div>
              <div class="flex flex-col gap-1 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0">
                <button @click="togglePin(note)" class="p-1 rounded hover:bg-surface-overlay text-text-muted hover:text-amber-400 transition-colors" :title="note.pinned ? 'Unpin' : 'Pin'">
                  <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z"/></svg>
                </button>
                <button @click="deleteNote(note.id)" class="p-1 rounded hover:bg-red-500/15 text-text-muted hover:text-red-400 transition-colors" title="Delete">
                  <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
                </button>
              </div>
            </div>
          </div>

          <!-- Empty state -->
          <div v-else-if="!addingNote" class="flex flex-col items-center justify-center py-16 text-center border border-dashed border-border rounded-xl">
            <div class="w-12 h-12 rounded-full bg-surface-overlay flex items-center justify-center mb-3">
              <svg class="w-6 h-6 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
              </svg>
            </div>
            <p class="text-sm font-medium text-text-secondary mb-1">No notes yet</p>
            <p class="text-xs text-text-muted mb-4">Add notes to track your application journey</p>
            <button @click="addingNote = true"
              class="flex items-center gap-2 px-4 py-2 bg-accent text-white text-sm font-medium rounded-lg hover:bg-accent/90 transition-colors">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              Add First Note
            </button>
          </div>

          <!-- Legacy personal notes (read-only if exists) -->
          <div v-if="noteText" class="mt-6">
            <div class="flex items-center justify-between mb-2">
              <p class="text-xs font-semibold text-text-muted uppercase tracking-wider">Legacy Notes</p>
              <div class="flex items-center gap-2">
                <span v-if="noteSaved" class="text-xs text-emerald-400">Saved ✓</span>
                <button v-if="!noteEditing" @click="noteEditing = true" class="text-xs text-accent hover:underline">Edit</button>
                <button v-else @click="saveNote" :disabled="noteSaving"
                  class="text-xs bg-accent text-white px-3 py-1 rounded-lg hover:bg-accent/90 disabled:opacity-50">
                  {{ noteSaving ? 'Saving…' : 'Save' }}
                </button>
                <button v-if="noteEditing" @click="noteEditing = false; noteText = company.personal_notes ?? ''"
                  class="text-xs text-text-muted hover:underline">Cancel</button>
              </div>
            </div>
            <textarea v-if="noteEditing" v-model="noteText" rows="6"
              class="w-full bg-surface-raised border border-accent rounded-xl p-4 text-sm text-text-primary outline-none resize-none font-mono"/>
            <div v-else class="bg-surface-raised border border-border rounded-xl p-4">
              <p class="text-sm text-text-secondary leading-relaxed whitespace-pre-wrap">{{ noteText }}</p>
            </div>
          </div>
        </div>

        <!-- ── FILES ── -->
        <div v-else-if="activeTab === 'Files'" :class="showEditor || company.status === 'Regenerating' ? 'flex gap-5 items-start' : 'max-w-xl'">
          <div class="bg-surface-raised border border-border rounded-xl p-5 flex-shrink-0" :class="showEditor || company.status === 'Regenerating' ? 'w-80' : ''">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Vault Files</p>
            <div class="space-y-2">
              <!-- CV PDF — opens in browser -->
              <div
                :class="['flex items-center gap-3 p-3 rounded-lg transition-colors', company.has_cv_pdf ? 'hover:bg-surface-overlay cursor-pointer group' : 'opacity-40 cursor-not-allowed']"
                @click="company.has_cv_pdf && (pdfViewer = 'cv')">
                <div class="w-9 h-9 rounded-lg bg-red-500/10 flex items-center justify-center flex-shrink-0">
                  <svg class="w-4 h-4 text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/></svg>
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-text-primary">KhawajaBilal_Uddin_CV.pdf</p>
                  <p class="text-xs text-text-muted">CV PDF · {{ company.has_cv_pdf ? 'Available — click to view' : 'Not found' }}</p>
                </div>
                <svg v-if="company.has_cv_pdf" class="w-4 h-4 text-text-muted opacity-0 group-hover:opacity-100 transition-opacity" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
              </div>

              <!-- Cover Letter PDF -->
              <div
                :class="['flex items-center gap-3 p-3 rounded-lg transition-colors', company.has_letter_pdf ? 'hover:bg-surface-overlay cursor-pointer group' : 'opacity-40 cursor-not-allowed']"
                @click="company.has_letter_pdf && (pdfViewer = 'letter')">
                <div class="w-9 h-9 rounded-lg bg-red-500/10 flex items-center justify-center flex-shrink-0">
                  <svg class="w-4 h-4 text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/></svg>
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-text-primary">KhawajaBilal_Uddin_CoverLetter.pdf</p>
                  <p class="text-xs text-text-muted">Cover Letter PDF · {{ company.has_letter_pdf ? 'Available — click to view' : 'Not found' }}</p>
                </div>
                <svg v-if="company.has_letter_pdf" class="w-4 h-4 text-text-muted opacity-0 group-hover:opacity-100 transition-opacity" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
              </div>

              <!-- compatibility-report.md -->
              <div class="flex items-center gap-3 p-3 rounded-lg"
                :class="company.has_report ? '' : 'opacity-40'">
                <div class="w-9 h-9 rounded-lg bg-blue-500/10 flex items-center justify-center flex-shrink-0">
                  <svg class="w-4 h-4 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/></svg>
                </div>
                <div>
                  <p class="text-sm font-medium text-text-primary">compatibility-report.md</p>
                  <p class="text-xs text-text-muted">Compatibility Report · {{ company.has_report ? 'Available' : 'Not found' }}</p>
                </div>
              </div>

              <!-- tailoring-notes.md -->
              <div class="flex items-center gap-3 p-3 rounded-lg"
                :class="company.has_notes ? '' : 'opacity-40'">
                <div class="w-9 h-9 rounded-lg bg-blue-500/10 flex items-center justify-center flex-shrink-0">
                  <svg class="w-4 h-4 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/></svg>
                </div>
                <div>
                  <p class="text-sm font-medium text-text-primary">tailoring-notes.md</p>
                  <p class="text-xs text-text-muted">Tailoring Notes · {{ company.has_notes ? 'Available' : 'Not found' }}</p>
                </div>
              </div>
            </div>

            <!-- Edit button -->
            <div v-if="company.has_content && company.status !== 'Regenerating'" class="mt-4 pt-4 border-t border-border flex justify-end">
              <button @click="showEditor = !showEditor"
                :class="['flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg transition-colors', showEditor ? 'bg-surface-overlay text-text-muted' : 'bg-accent/15 text-accent hover:bg-accent/25 border border-accent/30']">
                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
                {{ showEditor ? 'Close Editor' : 'Edit Content' }}
              </button>
            </div>
          </div>

          <!-- Content Editor -->
          <div v-if="showEditor || company.status === 'Regenerating'" class="flex-1 min-w-0">
            <ContentEditor
              :company-name="company.name"
              :job-url="company.job_url"
              :is-regenerating="company.status === 'Regenerating'"
              @regenerated="company.status = 'Regenerating'; showEditor = false"
            />
          </div>
        </div>

        <!-- ── INTERVIEWS ── -->
        <div v-else-if="activeTab === 'Interviews'" class="max-w-2xl space-y-4">

          <!-- Summary bar -->
          <div v-if="localInterviews.length" class="flex items-center gap-3 px-4 py-3 bg-surface-raised border border-border rounded-xl">
            <span class="text-xs text-text-muted font-medium">{{ localInterviews.length }} round{{ localInterviews.length > 1 ? 's' : '' }}</span>
            <span class="w-px h-3 bg-border"/>
            <span class="text-xs text-emerald-400 font-medium">{{ localInterviews.filter(i=>i.outcome==="Pass").length }} passed</span>
            <span class="text-xs text-red-400 font-medium">{{ localInterviews.filter(i=>i.outcome==="Fail").length }} failed</span>
            <span v-if="localInterviews.filter(i=>i.outcome==='Pending').length" class="text-xs text-amber-400 font-medium">{{ localInterviews.filter(i=>i.outcome==="Pending").length }} pending</span>
            <div class="flex-1"/>
            <button @click="addingInterview = !addingInterview"
              :class="['flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg transition-colors', addingInterview ? 'bg-surface-overlay text-text-muted' : 'bg-accent/15 text-accent hover:bg-accent/25 border border-accent/30']">
              <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              {{ addingInterview ? 'Cancel' : 'Add Round' }}
            </button>
          </div>

          <!-- Timeline -->
          <div v-if="localInterviews.length" class="relative">
            <!-- Vertical line -->
            <div class="absolute left-5 top-5 bottom-5 w-px bg-border"/>

            <div class="space-y-3">
              <div v-for="(iv, idx) in localInterviews" :key="iv.id"
                class="relative flex gap-4 group">
                <!-- Timeline dot -->
                <div class="flex-shrink-0 w-10 flex justify-center pt-3.5 z-10">
                  <div :class="['w-3 h-3 rounded-full border-2 border-surface-raised', TYPE_STYLE[iv.type]?.dot ?? 'bg-slate-500']"/>
                </div>

                <!-- Card -->
                <div class="flex-1 bg-surface-raised border border-border rounded-xl p-4 hover:border-border transition-colors">
                  <div class="flex items-start justify-between gap-3 mb-3">
                    <div class="flex items-center gap-2 flex-wrap">
                      <!-- Round number -->
                      <span class="text-[10px] font-mono text-text-muted">#{{ idx + 1 }}</span>
                      <!-- Type badge -->
                      <span :class="['text-xs font-semibold px-2.5 py-1 rounded-full border', TYPE_STYLE[iv.type]?.badge ?? 'bg-slate-500/15 text-slate-400 border-slate-500/30']">
                        {{ iv.type }}
                      </span>
                      <!-- Date -->
                      <span v-if="iv.date" class="text-xs text-text-muted font-mono">{{ iv.date }}</span>
                    </div>
                    <div class="flex items-center gap-1.5 flex-shrink-0">
                      <!-- Clickable outcome buttons -->
                      <button v-for="o in INTERVIEW_OUTCOMES" :key="o"
                        @click="updateInterviewOutcome(idx, o)"
                        :class="['text-[10px] font-semibold px-2 py-0.5 rounded-full transition-all',
                          iv.outcome === o
                            ? (OUTCOME_STYLE[o]?.selected ?? 'bg-slate-500 text-white')
                            : 'bg-transparent text-text-muted opacity-0 group-hover:opacity-60 hover:!opacity-100 ' + (OUTCOME_STYLE[o]?.unselected ?? '')]">
                        {{ o === 'Pass' ? '✓' : o === 'Fail' ? '✗' : '⏳' }}
                      </button>
                      <!-- Delete -->
                      <button @click="deleteInterview(idx)"
                        class="opacity-0 group-hover:opacity-100 transition-opacity p-1 rounded-lg hover:bg-red-500/15 text-text-muted hover:text-red-400">
                        <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
                      </button>
                    </div>
                  </div>
                  <p class="text-sm text-text-secondary leading-relaxed">{{ iv.notes }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- Empty state -->
          <div v-if="!localInterviews.length && !addingInterview"
            class="flex flex-col items-center justify-center py-16 text-center border border-dashed border-border rounded-xl">
            <div class="w-12 h-12 rounded-full bg-surface-overlay flex items-center justify-center mb-3">
              <svg class="w-6 h-6 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
              </svg>
            </div>
            <p class="text-sm font-medium text-text-secondary mb-1">No interviews recorded yet</p>
            <p class="text-xs text-text-muted mb-4">Log each round as it happens</p>
            <button @click="addingInterview = true"
              class="flex items-center gap-2 px-4 py-2 bg-accent text-white text-sm font-medium rounded-lg hover:bg-accent/90 transition-colors">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              Add First Round
            </button>
          </div>

          <!-- Add interview form -->
          <Transition enter-active-class="transition-all duration-200 ease-out" enter-from-class="opacity-0 -translate-y-2" enter-to-class="opacity-100 translate-y-0">
            <div v-if="addingInterview" class="bg-surface-raised border border-accent/40 rounded-xl p-5 space-y-4">
              <p class="text-sm font-semibold text-text-primary">Log Interview Round</p>

              <!-- Date + Type row -->
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="block text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-1.5">Date</label>
                  <input v-model="newInterview.date" type="date"
                    class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary outline-none focus:border-accent transition-colors"/>
                </div>
                <div>
                  <label class="block text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-1.5">Type</label>
                  <div class="flex flex-wrap gap-1.5">
                    <button v-for="t in INTERVIEW_TYPES" :key="t"
                      @click="newInterview.type = t"
                      :class="['px-2.5 py-1 text-xs font-medium rounded-full border transition-colors',
                        newInterview.type === t
                          ? TYPE_STYLE[t]?.badge ?? 'bg-slate-500/15 text-slate-400 border-slate-500/30'
                          : 'bg-surface-overlay text-text-muted border-border hover:border-accent/40']">
                      {{ t }}
                    </button>
                  </div>
                  <input v-if="newInterview.type === 'Other'" v-model="customInterviewType" type="text"
                    placeholder="e.g. Hiring Manager, Impact Day..."
                    class="mt-2 w-full bg-surface-overlay border border-border rounded-lg px-3 py-2 text-sm text-text-primary placeholder:text-text-muted outline-none focus:border-accent transition-colors"/>
                </div>
              </div>

              <!-- Notes -->
              <div>
                <label class="block text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-1.5">Notes</label>
                <input v-model="newInterview.notes" type="text"
                  placeholder="Interviewer name, topics covered, impressions..."
                  class="w-full bg-surface-overlay border border-border rounded-lg px-3 py-2.5 text-sm text-text-primary placeholder:text-text-muted outline-none focus:border-accent transition-colors"/>
              </div>

              <!-- Outcome -->
              <div>
                <label class="block text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-1.5">Outcome</label>
                <div class="flex gap-2">
                  <button v-for="o in INTERVIEW_OUTCOMES" :key="o"
                    @click="newInterview.outcome = o"
                    :class="['flex-1 py-2 text-sm font-semibold rounded-lg transition-all',
                      newInterview.outcome === o
                        ? OUTCOME_STYLE[o]?.selected
                        : OUTCOME_STYLE[o]?.unselected]">
                    {{ o === 'Pass' ? '✓ Pass' : o === 'Fail' ? '✗ Fail' : '⏳ Pending' }}
                  </button>
                </div>
              </div>

              <!-- Error + Save -->
              <div class="flex items-center justify-between gap-3 pt-1">
                <p v-if="ivError" class="text-xs text-red-400">{{ ivError }}</p>
                <div v-else/>
                <div class="flex gap-2">
                  <button @click="addingInterview = false; ivError = ''"
                    class="px-4 py-2 text-xs text-text-muted hover:text-text-primary bg-surface-overlay rounded-lg transition-colors">
                    Cancel
                  </button>
                  <button @click="saveInterview" :disabled="ivSaving"
                    class="flex items-center gap-2 px-4 py-2 text-xs font-semibold bg-accent text-white rounded-lg hover:bg-accent/90 disabled:opacity-50 transition-colors">
                    <svg v-if="ivSaving" class="w-3.5 h-3.5 animate-spin" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
                    {{ ivSaving ? 'Saving…' : 'Save Round' }}
                  </button>
                </div>
              </div>
            </div>
          </Transition>

        </div>

        <!-- ── PIPELINE ── -->
        <div v-else-if="activeTab === 'Journey'" class="grid grid-cols-3 gap-6">

          <!-- LEFT: Timeline (2 cols) -->
          <div class="col-span-2 space-y-5">
            <!-- Header + Progress -->
            <div>
              <div class="flex items-center justify-between mb-4">
                <p class="text-xs font-semibold text-text-muted uppercase tracking-wider">Application Journey</p>
                <div class="flex items-center gap-2">
                  <div :class="['w-2 h-2 rounded-full', currentStage === 'Rejected' ? 'bg-red-500 shadow-[0_0_8px_rgba(239,68,68,0.4)]' : currentStage === 'Offer' ? 'bg-amber-400 shadow-[0_0_8px_rgba(251,191,36,0.4)]' : 'bg-accent shadow-[0_0_8px_rgba(99,102,241,0.4)]']"/>
                  <span :class="['text-xs font-semibold', STAGE_COLORS[currentStage]?.text ?? 'text-text-muted']">{{ currentStage }}</span>
                </div>
              </div>
              <div class="h-1 bg-surface-overlay rounded-full overflow-hidden">
                <div class="h-full rounded-full transition-all duration-500"
                  :class="currentStage === 'Rejected' ? 'bg-gradient-to-r from-indigo-500 via-violet-500 to-red-500' : 'bg-gradient-to-r from-indigo-500 to-violet-500'"
                  :style="{ width: `${Math.min(100, ((timelineStages.indexOf(currentStage) + 1) / timelineStages.length) * 100)}%` }"/>
              </div>
            </div>

            <!-- Timeline -->
            <div class="relative ml-3">
              <div class="absolute left-[10px] top-3 bottom-3 w-0.5 rounded-full"
                :class="currentStage === 'Rejected' ? 'bg-gradient-to-b from-indigo-500 via-violet-500 to-red-500' : 'bg-gradient-to-b from-indigo-500 to-violet-500/30'"/>

              <div v-for="stage in timelineStages" :key="stage" class="relative pb-6 last:pb-0">
                <div class="flex items-center gap-4">
                  <div class="relative z-10 flex-shrink-0">
                    <div v-if="stage === 'Rejected'"
                      class="w-[22px] h-[22px] rounded-full bg-red-500 border-[3px] border-surface flex items-center justify-center shadow-[0_0_12px_rgba(239,68,68,0.3)]">
                      <svg class="w-2.5 h-2.5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M6 18L18 6M6 6l12 12"/></svg>
                    </div>
                    <div v-else-if="isStageCompleted(stage)"
                      :class="['w-[22px] h-[22px] rounded-full border-[3px] border-surface flex items-center justify-center',
                        stage === currentStage ? 'bg-accent shadow-[0_0_12px_rgba(99,102,241,0.4)]' : 'bg-indigo-500']">
                      <svg class="w-2.5 h-2.5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/></svg>
                    </div>
                    <div v-else class="w-[22px] h-[22px] rounded-full bg-surface-overlay border-2 border-border"/>
                  </div>

                  <div class="flex items-center gap-2.5">
                    <span :class="['text-sm font-bold', stage === 'Rejected' ? 'text-red-400' : isStageCompleted(stage) ? 'text-text-primary' : 'text-text-muted']">
                      {{ stage }}
                    </span>
                    <span v-if="notesForStage(stage).length" :class="['text-[10px] px-1.5 py-0.5 rounded-full font-medium',
                      stage === 'Interview' ? 'bg-violet-500/15 text-violet-400'
                        : stage === 'Rejected' ? 'bg-red-500/15 text-red-400'
                        : 'bg-accent/15 text-accent']">
                      {{ notesForStage(stage).length }} note{{ notesForStage(stage).length > 1 ? 's' : '' }}
                    </span>
                  </div>
                </div>

                <div v-if="notesForStage(stage).length" class="ml-[38px] mt-2 space-y-1.5">
                  <div v-for="note in notesForStage(stage)" :key="note.id"
                    :class="['flex gap-2.5 items-start p-3 rounded-xl border transition-colors',
                      NOTE_STYLE[note.category]?.bg ?? 'bg-surface-raised',
                      NOTE_STYLE[note.category]?.border ?? 'border-border']">
                    <span class="text-sm flex-shrink-0 mt-0.5">{{ NOTE_STYLE[note.category]?.icon ?? '💡' }}</span>
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2 mb-1">
                        <span :class="['text-[9px] font-semibold uppercase tracking-wider px-1.5 py-0.5 rounded',
                          `${NOTE_STYLE[note.category]?.bg} ${NOTE_STYLE[note.category]?.color}`]">
                          {{ note.category }}
                        </span>
                        <span class="text-[9px] text-text-muted">{{ formatNoteDate(note.created_at) }}</span>
                        <span v-if="note.pinned" class="text-[9px] px-1.5 py-0.5 bg-amber-500/10 text-amber-400 rounded font-medium">📌 Pinned</span>
                      </div>
                      <p class="text-xs text-text-secondary leading-relaxed whitespace-pre-wrap">{{ note.content }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- RIGHT: Summary panel (1 col) -->
          <div class="col-span-1 space-y-4">

            <!-- Status card -->
            <div class="bg-surface-raised border border-border rounded-xl p-5">
              <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-4">Status</p>
              <div class="flex items-center gap-3 mb-4">
                <div :class="['w-10 h-10 rounded-full flex items-center justify-center',
                  currentStage === 'Rejected' ? 'bg-red-500/15' : currentStage === 'Offer' ? 'bg-amber-500/15' : 'bg-accent/15']">
                  <span :class="['text-lg font-bold', STAGE_COLORS[currentStage]?.text ?? 'text-text-muted']">
                    {{ currentStage === 'Rejected' ? '✗' : currentStage === 'Offer' ? '★' : '→' }}
                  </span>
                </div>
                <div>
                  <p :class="['text-sm font-bold', STAGE_COLORS[currentStage]?.text ?? 'text-text-muted']">{{ currentStage }}</p>
                  <p class="text-[10px] text-text-muted">Current stage</p>
                </div>
              </div>
              <div class="grid grid-cols-2 gap-3">
                <div class="p-3 rounded-lg bg-surface-overlay border border-border">
                  <p class="text-xl font-bold text-text-primary">{{ daysInPipeline ?? '—' }}</p>
                  <p class="text-[10px] text-text-muted uppercase tracking-wider mt-0.5">Days in pipeline</p>
                </div>
                <div class="p-3 rounded-lg bg-surface-overlay border border-border">
                  <p class="text-xl font-bold text-text-primary">{{ localNotes.length }}</p>
                  <p class="text-[10px] text-text-muted uppercase tracking-wider mt-0.5">Total notes</p>
                </div>
                <div class="p-3 rounded-lg bg-surface-overlay border border-border">
                  <p class="text-xl font-bold text-violet-400">{{ company.interviews?.length ?? 0 }}</p>
                  <p class="text-[10px] text-text-muted uppercase tracking-wider mt-0.5">Interviews</p>
                </div>
                <div class="p-3 rounded-lg bg-surface-overlay border border-border">
                  <p class="text-xl font-bold text-text-primary">{{ company.match_pct ?? '—' }}<span v-if="company.match_pct" class="text-xs text-text-muted">%</span></p>
                  <p class="text-[10px] text-text-muted uppercase tracking-wider mt-0.5">Match score</p>
                </div>
              </div>
            </div>

            <!-- Key dates -->
            <div class="bg-surface-raised border border-border rounded-xl p-5">
              <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-3">Key Dates</p>
              <div class="space-y-2.5">
                <div v-if="company.applied_date" class="flex items-center justify-between">
                  <span class="text-xs text-text-muted">Applied</span>
                  <span class="text-xs font-medium text-text-primary font-mono">{{ company.applied_date }}</span>
                </div>
                <div v-for="iv in [...(company.interviews ?? [])].sort((a, b) => a.date.localeCompare(b.date))" :key="iv.id" class="flex items-center justify-between">
                  <div class="flex items-center gap-1.5">
                    <span :class="['w-1.5 h-1.5 rounded-full flex-shrink-0', TYPE_STYLE[iv.type]?.dot ?? 'bg-slate-500']"/>
                    <span class="text-xs text-text-muted">{{ iv.type }} Interview</span>
                  </div>
                  <div class="flex items-center gap-2">
                    <span class="text-xs font-medium text-text-primary font-mono">{{ iv.date }}</span>
                    <span :class="['text-[9px] font-semibold', iv.outcome === 'Pass' ? 'text-emerald-400' : iv.outcome === 'Fail' ? 'text-red-400' : 'text-amber-400']">
                      {{ iv.outcome === 'Pass' ? '✓' : iv.outcome === 'Fail' ? '✗' : '⏳' }}
                    </span>
                  </div>
                </div>
                <div v-if="company.follow_up_date" class="flex items-center justify-between">
                  <span class="text-xs text-text-muted">Follow-up</span>
                  <span :class="['text-xs font-medium font-mono',
                    company.follow_up_date <= new Date().toISOString().split('T')[0] ? 'text-amber-400' : 'text-text-primary']">
                    {{ company.follow_up_date }}
                  </span>
                </div>
                <div v-if="currentStage === 'Rejected'" class="flex items-center justify-between">
                  <span class="text-xs text-red-400">Rejected</span>
                  <span class="text-xs font-medium text-red-400 font-mono">{{ company.synced_at?.split('T')[0] ?? '—' }}</span>
                </div>
                <div v-if="salaryDisplay" class="flex items-center justify-between pt-1 border-t border-border">
                  <span class="text-xs text-text-muted">{{ salaryDisplay.label }}</span>
                  <span class="text-xs font-medium text-emerald-400 font-mono">{{ salaryDisplay.value }}</span>
                </div>
              </div>
            </div>

            <!-- Notes breakdown -->
            <div class="bg-surface-raised border border-border rounded-xl p-5">
              <p class="text-[10px] font-semibold text-text-muted uppercase tracking-wider mb-3">Notes by Category</p>
              <div class="space-y-2">
                <div v-for="cat in NOTE_CATEGORIES" :key="cat" class="flex items-center gap-2.5">
                  <span class="text-sm flex-shrink-0">{{ NOTE_STYLE[cat]?.icon }}</span>
                  <span class="text-xs text-text-secondary flex-1">{{ cat }}</span>
                  <span :class="['text-xs font-bold tabular-nums', noteCountsByCategory[cat] ? (NOTE_STYLE[cat]?.color ?? 'text-text-primary') : 'text-text-muted']">
                    {{ noteCountsByCategory[cat] }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Quick add note -->
            <button @click="activeTab = 'My Notes'; addingNote = true"
              class="w-full flex items-center justify-center gap-2 px-4 py-3 bg-accent/10 text-accent text-sm font-medium rounded-xl border border-accent/20 hover:bg-accent/20 transition-colors">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
              Add Note
            </button>

          </div>
        </div>

      </div>
    </div>
  </div>
</template>

<style>
/* Styles for Flask-rendered markdown HTML */
.prose-report h1, .prose-report h2, .prose-report h3 {
  color: var(--color-text-primary);
  font-weight: 600;
  margin-top: 1rem;
  margin-bottom: 0.5rem;
}
.prose-report h1 { font-size: 1rem; }
.prose-report h2 { font-size: 0.9rem; }
.prose-report h3 { font-size: 0.85rem; color: var(--color-text-secondary); }
.prose-report p  { margin-bottom: 0.5rem; color: var(--color-text-secondary); font-size: 0.875rem; }
.prose-report ul { list-style: disc; padding-left: 1.25rem; margin-bottom: 0.5rem; }
.prose-report li { color: var(--color-text-secondary); font-size: 0.875rem; margin-bottom: 0.2rem; }
.prose-report strong { color: var(--color-text-primary); font-weight: 600; }
.prose-report em { color: var(--color-text-muted); }
.prose-report code { background: var(--color-surface-overlay); color: var(--color-accent); padding: 0.1rem 0.3rem; border-radius: 4px; font-size: 0.8rem; }
.prose-report table { width: 100%; border-collapse: collapse; font-size: 0.8rem; margin: 0.75rem 0; }
.prose-report th { background: var(--color-surface-overlay); color: var(--color-text-primary); font-weight: 600; text-align: left; padding: 0.4rem 0.6rem; border: 1px solid var(--color-border); }
.prose-report td { color: var(--color-text-secondary); padding: 0.35rem 0.6rem; border: 1px solid var(--color-border); }
.prose-report tr:nth-child(even) td { background: var(--color-surface-overlay); }
.prose-report hr { border: none; border-top: 1px solid var(--color-border); margin: 0.75rem 0; }
.prose-report blockquote { border-left: 3px solid var(--color-accent); padding-left: 0.75rem; color: var(--color-text-muted); margin: 0.5rem 0; font-style: italic; }
</style>
