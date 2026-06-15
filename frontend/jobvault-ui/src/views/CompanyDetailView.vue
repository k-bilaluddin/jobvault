<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppHeader from '@/components/layout/AppHeader.vue'
import CompanyAvatar from '@/components/common/CompanyAvatar.vue'
import RecommendBadge from '@/components/common/RecommendBadge.vue'
import MatchRing from '@/components/common/MatchRing.vue'
import { useCompanies } from '@/composables/useCompanies'
import { OUTCOME_COLORS } from '@/utils/score'
import { PIPELINE_STAGES } from '@/types'
import type { ApplicationStage } from '@/types'

const route  = useRoute()
const router = useRouter()
const { getByName, loading } = useCompanies()

const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5100'

const companyName = computed(() => decodeURIComponent(route.params.name as string))
const company     = computed(() => getByName(companyName.value))

const TABS = ['Compatibility Report', 'Tailoring Notes', 'Details', 'My Notes', 'Files', 'Interviews', 'Pipeline'] as const
type Tab = typeof TABS[number]
const activeTab = ref<Tab>('Compatibility Report')

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
    const res = await fetch(`${API_BASE}/api/company/${encodeURIComponent(companyName.value)}/stage`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ stage: newStage }),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
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

// ── Compatibility report HTML (from Flask) ────────────────────
const reportHtml   = ref('')
const reportLoading = ref(false)
const reportLoaded  = ref(false)

async function loadReport() {
  if (reportLoaded.value) return
  reportLoading.value = true
  try {
    const res = await fetch(`${API_BASE}/api/company/${encodeURIComponent(companyName.value)}/report`)
    const data = await res.json()
    reportHtml.value = data.html ?? ''
    reportLoaded.value = true
  } catch {
    reportHtml.value = '<p class="text-red-400 text-sm">Failed to load report. Is Flask running?</p>'
    reportLoaded.value = true
  } finally {
    reportLoading.value = false
  }
}

// ── Tailoring notes HTML (from Flask) ────────────────────────
const notesHtml    = ref('')
const notesLoading  = ref(false)
const notesLoaded   = ref(false)

async function loadNotes() {
  if (notesLoaded.value) return
  notesLoading.value = true
  try {
    const res = await fetch(`${API_BASE}/api/company/${encodeURIComponent(companyName.value)}/notes`)
    const data = await res.json()
    notesHtml.value = data.html ?? ''
    notesLoaded.value = true
  } catch {
    notesHtml.value = '<p class="text-red-400 text-sm">Failed to load notes. Is Flask running?</p>'
    notesLoaded.value = true
  } finally {
    notesLoading.value = false
  }
}

// Load on tab switch
watch(activeTab, (tab) => {
  if (tab === 'Compatibility Report') loadReport()
  if (tab === 'Tailoring Notes') loadNotes()
})

// Load report on mount if it's the default tab
watch(() => company.value, (c) => {
  if (c && activeTab.value === 'Compatibility Report') loadReport()
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
    await fetch(`${API_BASE}/api/company/${encodeURIComponent(companyName.value)}/personal-notes`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ notes: noteText.value }),
    })
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
const pdfUrl = computed(() =>
  pdfViewer.value
    ? `${API_BASE}/api/company/${encodeURIComponent(companyName.value)}/pdf/${pdfViewer.value}`
    : ''
)

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
  if (v.includes('-')) {
    const [lo, hi] = v.split('-').map(n => Math.round(+n / 1000) + 'k')
    return `€${lo} – €${hi}`
  }
  const n = parseInt(v)
  return isNaN(n) ? v : `€${Math.round(n / 1000)}k`
}

const INTERVIEW_TYPE_COLOR: Record<string, string> = {
  HR:        'bg-blue-500/15 text-blue-400',
  Technical: 'bg-violet-500/15 text-violet-400',
  Onsite:    'bg-amber-500/15 text-amber-400',
  Final:     'bg-emerald-500/15 text-emerald-400',
  Phone:     'bg-sky-500/15 text-sky-400',
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
      </div>

      <!-- Tabs -->
      <div class="px-6 border-b border-border flex gap-0 overflow-x-auto flex-shrink-0">
        <button v-for="tab in TABS" :key="tab" @click="activeTab = tab"
          class="px-4 py-3 text-sm font-medium whitespace-nowrap border-b-2 transition-colors flex-shrink-0"
          :class="activeTab === tab ? 'border-accent text-accent' : 'border-transparent text-text-muted hover:text-text-primary'">
          {{ tab }}
          <span v-if="tab === 'Interviews' && (company.interviews?.length ?? 0) > 0"
            class="ml-1 text-[10px] bg-violet-500/20 text-violet-400 px-1 rounded">{{ company.interviews.length }}</span>
        </button>
      </div>

      <!-- Tab content -->
      <div class="flex-1 overflow-y-auto p-6">

        <!-- ── COMPATIBILITY REPORT ── -->
        <div v-if="activeTab === 'Compatibility Report'" class="grid grid-cols-3 gap-6">
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
        <div v-else-if="activeTab === 'Tailoring Notes'" class="max-w-3xl">
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
        <div v-else-if="activeTab === 'Details'" class="max-w-2xl space-y-4">
          <div class="bg-surface-raised border border-border rounded-xl divide-y divide-border">
            <div v-for="row in [
              { label: 'Company',      value: company.name },
              { label: 'Stage',        value: company.stage },
              { label: 'Applied',      value: company.applied ? 'Yes' : 'No' },
              { label: 'Applied Date', value: company.applied_date || '—' },
              { label: 'Source',       value: company.source || 'Direct' },
              { label: 'Follow-up',    value: company.follow_up_date || '—' },
              { label: 'Match %',      value: company.match_pct !== null ? company.match_pct + '%' : '—' },
              { label: 'Verdict',      value: company.recommend || '—' },
              { label: 'Has Report',   value: company.has_report ? '✓ Yes' : '✗ No' },
              { label: 'Has Notes',    value: company.has_notes  ? '✓ Yes' : '✗ No' },
              { label: 'Has CV PDF',   value: company.has_cv_pdf ? '✓ Yes' : '✗ No' },
              { label: 'Has Letter',   value: company.has_letter_pdf ? '✓ Yes' : '✗ No' },
            ]" :key="row.label" class="flex items-center gap-4 px-5 py-3">
              <span class="text-xs text-text-muted w-28 flex-shrink-0">{{ row.label }}</span>
              <span class="text-sm text-text-primary">{{ row.value }}</span>
            </div>
          </div>

          <div v-if="company.recruiter?.name || company.recruiter?.email" class="bg-surface-raised border border-border rounded-xl p-4">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-3">Recruiter</p>
            <div class="space-y-2 text-xs">
              <div v-if="company.recruiter.name" class="flex gap-3"><span class="text-text-muted w-16">Name</span><span class="text-text-primary">{{ company.recruiter.name }}</span></div>
              <div v-if="company.recruiter.email" class="flex gap-3"><span class="text-text-muted w-16">Email</span><a :href="`mailto:${company.recruiter.email}`" class="text-accent hover:underline">{{ company.recruiter.email }}</a></div>
              <div v-if="company.recruiter.linkedin" class="flex gap-3"><span class="text-text-muted w-16">LinkedIn</span><a :href="company.recruiter.linkedin" target="_blank" class="text-accent hover:underline">{{ company.recruiter.linkedin }}</a></div>
            </div>
          </div>

          <div class="bg-surface-raised border border-border rounded-xl p-4">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-3">Salary</p>
            <div class="grid grid-cols-2 gap-3">
              <div v-for="(val, key) in company.salary" :key="key" class="text-xs">
                <p class="text-text-muted capitalize mb-0.5">{{ key }}</p>
                <p class="text-text-primary font-medium">{{ val ? formatSal(val) : '—' }}</p>
              </div>
            </div>
          </div>
        </div>

        <!-- ── MY NOTES ── -->
        <div v-else-if="activeTab === 'My Notes'" class="max-w-2xl space-y-3">
          <div class="flex items-center justify-between">
            <p class="text-sm font-semibold text-text-primary">Personal Notes</p>
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
          <textarea v-if="noteEditing" v-model="noteText" rows="10"
            class="w-full bg-surface-raised border border-accent rounded-xl p-4 text-sm text-text-primary outline-none resize-none font-mono"/>
          <div v-else class="bg-surface-raised border border-border rounded-xl p-5 min-h-[120px]">
            <p v-if="noteText" class="text-sm text-text-secondary leading-relaxed whitespace-pre-wrap">{{ noteText }}</p>
            <p v-else class="text-sm text-text-muted italic">No personal notes. Click Edit to add.</p>
          </div>
        </div>

        <!-- ── FILES ── -->
        <div v-else-if="activeTab === 'Files'" class="max-w-xl">
          <div class="bg-surface-raised border border-border rounded-xl p-5">
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
          </div>
        </div>

        <!-- ── INTERVIEWS ── -->
        <div v-else-if="activeTab === 'Interviews'" class="max-w-2xl">
          <div v-if="!company.interviews?.length" class="flex flex-col items-center justify-center py-16 opacity-50 text-center">
            <p class="text-3xl mb-2">📅</p>
            <p class="text-sm text-text-muted">No interviews recorded</p>
          </div>
          <div v-else class="space-y-3">
            <div v-for="iv in company.interviews" :key="iv.id" class="bg-surface-raised border border-border rounded-xl p-4">
              <div class="flex items-start justify-between gap-3 mb-2">
                <div class="flex items-center gap-2 flex-wrap">
                  <span :class="['text-xs font-semibold px-2 py-0.5 rounded-full', INTERVIEW_TYPE_COLOR[iv.type] ?? 'bg-slate-500/15 text-slate-400']">{{ iv.type }}</span>
                  <span v-if="iv.date" class="text-xs text-text-muted font-mono">{{ iv.date }}</span>
                  <span v-else class="text-xs text-text-muted italic">No date</span>
                </div>
                <span :class="['text-xs font-semibold px-2 py-0.5 rounded-full flex-shrink-0', OUTCOME_COLORS[iv.outcome] ?? 'bg-slate-500/15 text-slate-400']">{{ iv.outcome }}</span>
              </div>
              <p class="text-sm text-text-secondary">{{ iv.notes }}</p>
            </div>
            <div class="bg-surface-overlay border border-border rounded-xl p-4">
              <div class="flex items-center gap-4 text-xs">
                <span class="text-text-muted">{{ company.interviews.length }} round{{ company.interviews.length > 1 ? 's' : '' }}</span>
                <span class="text-emerald-400">{{ company.interviews.filter(i=>i.outcome==='Pass').length }} passed</span>
                <span class="text-red-400">{{ company.interviews.filter(i=>i.outcome==='Fail').length }} failed</span>
                <span v-if="company.interviews.filter(i=>i.outcome==='Pending').length" class="text-amber-400">{{ company.interviews.filter(i=>i.outcome==='Pending').length }} pending</span>
              </div>
            </div>
          </div>
        </div>

        <!-- ── PIPELINE ── -->
        <div v-else-if="activeTab === 'Pipeline'" class="max-w-sm">
          <div class="bg-surface-raised border border-border rounded-xl p-5">
            <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Application Timeline</p>
            <ol class="space-y-0">
              <li v-for="(stage, i) in (['Ready to Apply','Applied','Interview','Offer'] as ApplicationStage[])" :key="stage" class="flex items-start gap-3">
                <div class="flex flex-col items-center">
                  <div :class="['w-6 h-6 rounded-full border-2 flex items-center justify-center flex-shrink-0',
                    PIPELINE_STAGES.indexOf(currentStage) >= PIPELINE_STAGES.indexOf(stage)
                      ? 'bg-accent border-accent' : 'bg-surface-overlay border-border']">
                    <svg v-if="PIPELINE_STAGES.indexOf(currentStage) >= PIPELINE_STAGES.indexOf(stage)" class="w-3 h-3 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                    </svg>
                  </div>
                  <div v-if="i < 3" class="w-px h-8 mt-1" :class="PIPELINE_STAGES.indexOf(currentStage) > PIPELINE_STAGES.indexOf(stage) ? 'bg-accent/40' : 'bg-border'"/>
                </div>
                <div class="pb-8">
                  <p class="text-sm font-medium" :class="PIPELINE_STAGES.indexOf(currentStage) >= PIPELINE_STAGES.indexOf(stage) ? 'text-text-primary' : 'text-text-muted'">{{ stage }}</p>
                </div>
              </li>
              <li v-if="currentStage === 'Rejected'" class="flex items-start gap-3">
                <div class="w-6 h-6 rounded-full bg-red-500/20 border-2 border-red-500 flex items-center justify-center flex-shrink-0">
                  <svg class="w-3 h-3 text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M6 18L18 6M6 6l12 12"/></svg>
                </div>
                <p class="text-sm font-medium text-red-400">Rejected</p>
              </li>
            </ol>
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
