<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import AppHeader from '@/components/layout/AppHeader.vue'
import CompanyAvatar from '@/components/common/CompanyAvatar.vue'
import RecommendBadge from '@/components/common/RecommendBadge.vue'
import ApplicationRowActions from '@/components/applications/ApplicationRowActions.vue'
import { STAGE_COLORS, matchPctColor, matchPctBar } from '@/utils/score'
import { PIPELINE_STAGES } from '@/types'
import type { ApplicationStage, Company } from '@/types'
import { api } from '@/api'

const router = useRouter()

// ── Pagination state ────────────────────────────────────────
const companies = ref<Company[]>([])
const loading = ref(false)
const page = ref(1)
const pageSize = 10
const totalCount = ref(0)
const totalPages = ref(0)
const stageCounts = ref<Record<string, number>>({})

// ── Filters & sort ──────────────────────────────────────────
const search = ref('')
const filterStage = ref<ApplicationStage | 'All'>('All')

type SortKey = 'name' | 'synced_at' | 'applied_date' | 'stage' | 'match_pct' | 'salary'
const sortKey = ref<SortKey>('synced_at')
const sortDir = ref<'asc' | 'desc'>('desc')

// ── Fetch ───────────────────────────────────────────────────
let searchDebounce: ReturnType<typeof setTimeout> | null = null

async function fetchPage() {
  loading.value = true
  try {
    const params: Record<string, string | number> = {
      page: page.value,
      pageSize,
      sortBy: sortKey.value,
      sortDirection: sortDir.value,
    }
    if (search.value.trim()) params.search = search.value.trim()
    if (filterStage.value !== 'All') params.stage = filterStage.value

    const { data } = await api.get<{
      items: Company[]
      totalCount: number
      totalPages: number
      page: number
      pageSize: number
      stageCounts: Record<string, number>
    }>('/api/applications', { params })

    companies.value = data.items
    totalCount.value = data.totalCount
    totalPages.value = data.totalPages
    stageCounts.value = data.stageCounts
  } catch {
    companies.value = []
  } finally {
    loading.value = false
  }
}

onMounted(fetchPage)

watch([page, sortKey, sortDir, filterStage], fetchPage)

watch(search, () => {
  if (searchDebounce) clearTimeout(searchDebounce)
  searchDebounce = setTimeout(() => {
    page.value = 1
    fetchPage()
  }, 300)
})

watch(filterStage, () => { page.value = 1 })

// ── Tabs ────────────────────────────────────────────────────
const STAGE_TABS = ['All', ...PIPELINE_STAGES] as const

// ── Sort ────────────────────────────────────────────────────
function toggleSort(key: SortKey) {
  if (sortKey.value === key) { sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc' }
  else { sortKey.value = key; sortDir.value = 'desc' }
  page.value = 1
}

function sortIcon(key: SortKey) {
  if (sortKey.value !== key) return '↕'
  return sortDir.value === 'asc' ? '↑' : '↓'
}

// ── Pagination controls ─────────────────────────────────────
function goToPage(p: number) {
  if (p >= 1 && p <= totalPages.value) page.value = p
}

// ── Salary ───────────────────────────────────────────────────
function fmtSalary(c: Company): string {
  const v = c.salary?.advertised
  if (!v) return ''
  if (v.includes('-')) {
    const parts = v.split('-').map(n => { const x = parseInt(n); return x >= 1000 ? Math.round(x/1000)+'k' : '' })
    return parts[0] && parts[1] ? `€${parts[0]}–${parts[1]}` : ''
  }
  const n = parseInt(v)
  return n >= 1000 ? `€${Math.round(n/1000)}k` : ''
}

// ── Role ────────────────────────────────────────────────────
function roleStr(role: string | string[] | undefined): string {
  if (!role) return ''
  return Array.isArray(role) ? (role[0] ?? '') : role
}

// ── Follow-up ───────────────────────────────────────────────
const today = new Date().toISOString().split('T')[0]
function isFollowUpDue(c: Company): boolean {
  return !!c.follow_up_date && c.follow_up_date <= today
}

// ── Row actions ──────────────────────────────────────────────
const activeActionRow = ref<string | null>(null)
const stageUpdating   = ref<string | null>(null)

function toggleActions(name: string) {
  activeActionRow.value = activeActionRow.value === name ? null : name
}

function onDocClick() { activeActionRow.value = null }

async function quickStage(c: Company, stage: ApplicationStage) {
  stageUpdating.value = c.name
  activeActionRow.value = null
  try {
    await api.post(`/api/applications/${encodeURIComponent(c.name)}/stage`, { stage })
    await fetchPage()
  } finally {
    stageUpdating.value = null
  }
}

function openUrl(c: Company) {
  if (c.job_url) window.open(c.job_url, '_blank')
}

function openDetail(c: Company) {
  router.push(`/company/${encodeURIComponent(c.name)}`)
}

function copyEmail(email: string) {
  navigator.clipboard?.writeText(email).catch(() => {
    const el = document.createElement('textarea')
    el.value = email
    document.body.appendChild(el)
    el.select()
    document.execCommand('copy')
    document.body.removeChild(el)
  })
  activeActionRow.value = null
}

// ── Export CSV ───────────────────────────────────────────────
function exportCsv() {
  const header = ['Company', 'Role', 'Stage', 'Applied', 'Match%', 'Salary', 'Verdict'].join(',')
  const rows = companies.value.map(c =>
    [c.name, roleStr(c.role), c.stage, c.applied_date, c.match_pct ?? '', fmtSalary(c), c.recommend]
      .map(v => '"' + String(v ?? '').replace(/"/g, '""') + '"').join(',')
  )
  const csv = [header, ...rows].join('\n')
  const link = document.createElement('a')
  link.href = 'data:text/csv;charset=utf-8,' + encodeURIComponent(csv)
  link.download = 'applications.csv'
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
}
</script>

<template>
  <div class="flex flex-col h-full" @click="onDocClick">
    <AppHeader title="Applications" />
    <div class="flex-1 overflow-y-auto">

      <!-- Toolbar -->
      <div class="px-4 md:px-6 pt-5 pb-0 border-b border-border space-y-3">
        <div class="flex items-center gap-3">
          <div class="relative flex-1 min-w-0 sm:max-w-sm">
            <svg class="w-4 h-4 text-text-muted absolute left-3 top-1/2 -translate-y-1/2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
            </svg>
            <input v-model="search" type="text" placeholder="Search company or role..."
              class="w-full bg-surface-raised border border-border rounded-lg pl-9 pr-4 py-2 text-sm text-text-primary placeholder:text-text-muted outline-none focus:border-accent transition-colors"/>
          </div>
          <button @click.stop="exportCsv"
            class="flex items-center gap-1.5 px-3 py-2 text-xs text-text-muted hover:text-text-primary bg-surface-raised border border-border rounded-lg hover:border-accent/50 transition-colors flex-shrink-0">
            <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
            </svg>
            <span class="hidden sm:inline">Export CSV</span>
          </button>
        </div>

        <!-- Stage tabs -->
        <div class="flex items-end gap-0 overflow-x-auto">
          <button v-for="tab in STAGE_TABS" :key="tab"
            @click="filterStage = tab === 'All' ? 'All' : (tab as ApplicationStage)"
            class="px-3 py-2 text-xs font-medium whitespace-nowrap border-b-2 transition-colors flex-shrink-0"
            :class="filterStage === tab ? 'border-accent text-accent' : 'border-transparent text-text-muted hover:text-text-primary'">
            {{ tab }} <span class="ml-1 opacity-60">{{ stageCounts[tab] ?? 0 }}</span>
          </button>
        </div>
      </div>

      <!-- Table -->
      <div class="px-4 md:px-6 py-4">
        <div v-if="loading" class="space-y-2 animate-pulse">
          <div v-for="i in 8" :key="i" class="h-14 bg-surface-raised border border-border rounded-lg"/>
        </div>

        <div v-else-if="companies.length === 0" class="flex flex-col items-center justify-center py-20 text-center">
          <p class="text-4xl mb-3">🔍</p>
          <p class="text-text-secondary font-medium">No companies found</p>
        </div>

        <div v-else>
          <!-- Desktop table (md and up) -->
          <div class="hidden md:block">
            <!-- Header -->
            <div class="grid grid-cols-[2.5rem_1fr_7rem_9rem_6rem_6rem_6rem_7rem_2rem] gap-3 px-3 pb-2 text-[10px] font-semibold text-text-muted uppercase tracking-wider select-none">
              <div/>
              <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('name')">
                Company / Role <span class="opacity-40 font-mono">{{ sortIcon('name') }}</span>
              </button>
              <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('salary')">
                Salary <span class="opacity-40 font-mono">{{ sortIcon('salary') }}</span>
              </button>
              <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('stage')">
                Stage <span class="opacity-40 font-mono">{{ sortIcon('stage') }}</span>
              </button>
              <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('synced_at')">
                Received <span class="opacity-40 font-mono">{{ sortIcon('synced_at') }}</span>
              </button>
              <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('applied_date')">
                Applied <span class="opacity-40 font-mono">{{ sortIcon('applied_date') }}</span>
              </button>
              <button class="text-left flex items-center gap-1 hover:text-text-primary transition-colors" @click="toggleSort('match_pct')">
                Match <span class="opacity-40 font-mono">{{ sortIcon('match_pct') }}</span>
              </button>
              <div>Verdict</div>
              <div/>
            </div>

            <!-- Rows -->
            <div v-for="c in companies" :key="c.name" class="relative">
              <!-- Follow-up left border indicator -->
              <div v-if="isFollowUpDue(c)" class="absolute left-0 top-1 bottom-1 w-0.5 rounded-full bg-amber-400 z-10"/>

              <div @click="router.push(`/company/${encodeURIComponent(c.name)}`)"
                class="grid grid-cols-[2.5rem_1fr_7rem_9rem_6rem_6rem_6rem_7rem_2rem] gap-3 items-center px-3 py-2.5 rounded-lg border border-transparent cursor-pointer transition-all group"
                :class="[
                  stageUpdating === c.name ? 'opacity-50 pointer-events-none' : '',
                  isFollowUpDue(c)
                    ? 'hover:bg-amber-500/5 hover:border-amber-500/20'
                    : 'hover:bg-surface-raised hover:border-border'
                ]">

                <CompanyAvatar :name="c.name" size="sm"/>

                <!-- Company + role -->
                <div class="min-w-0">
                  <div class="flex items-center gap-1.5 min-w-0">
                    <p class="text-sm font-semibold text-text-primary truncate group-hover:text-accent transition-colors">{{ c.name }}</p>
                    <span v-if="c.has_report" class="w-1.5 h-1.5 rounded-full bg-emerald-500 flex-shrink-0" title="Has report"/>
                    <span v-if="(c.interviews?.length ?? 0) > 0" class="text-[10px] text-violet-400 flex-shrink-0 font-medium">{{ c.interviews.length }}✦</span>
                    <span v-if="isFollowUpDue(c)" class="text-[10px] text-amber-400 flex-shrink-0" title="Follow-up due">⏰</span>
                    <!-- Job posting link — always visible when URL exists -->
                    <a v-if="c.job_url" :href="c.job_url" target="_blank" @click.stop
                      class="flex-shrink-0 text-text-muted hover:text-blue-400 transition-colors"
                      title="Open job posting">
                      <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"/>
                      </svg>
                    </a>
                  </div>
                  <p v-if="roleStr(c.role)" class="text-xs text-text-muted truncate mt-0.5">{{ roleStr(c.role) }}</p>
                </div>

                <!-- Salary -->
                <span class="text-xs font-mono text-text-secondary">{{ fmtSalary(c) || '—' }}</span>

                <!-- Stage -->
                <div class="flex items-center gap-1.5 min-w-0">
                  <span :class="['w-1.5 h-1.5 rounded-full flex-shrink-0', STAGE_COLORS[c.stage as ApplicationStage]?.dot]"/>
                  <span :class="['text-xs font-medium truncate', STAGE_COLORS[c.stage as ApplicationStage]?.text]">{{ c.stage }}</span>
                </div>

                <!-- Received (synced_at) -->
                <span class="text-xs text-text-muted font-mono">{{ c.synced_at ? c.synced_at.slice(0,10) : '—' }}</span>

                <!-- Applied date -->
                <span class="text-xs text-text-muted font-mono">{{ c.applied_date || '—' }}</span>

                <!-- Match % -->
                <div v-if="c.match_pct !== null" class="flex items-center gap-1.5">
                  <div class="flex-1 h-1 bg-surface-overlay rounded-full overflow-hidden">
                    <div :class="['h-full rounded-full', matchPctBar(c.match_pct)]" :style="{ width: `${c.match_pct}%` }"/>
                  </div>
                  <span :class="['text-xs font-mono font-bold w-7 text-right flex-shrink-0', matchPctColor(c.match_pct)]">{{ c.match_pct }}%</span>
                </div>
                <span v-else class="text-xs text-text-muted">—</span>

                <!-- Verdict -->
                <RecommendBadge :recommend="c.recommend" size="sm"/>

                <ApplicationRowActions :company="c" :open="activeActionRow === c.name"
                  @toggle="toggleActions(c.name)"
                  @open-detail="openDetail(c)"
                  @open-url="openUrl(c)"
                  @copy-email="copyEmail(c.recruiter.email)"
                  @quick-stage="(stage) => quickStage(c, stage)"/>
              </div>
            </div>
          </div>

          <!-- Mobile cards (below md) -->
          <div class="md:hidden space-y-2">
            <div v-for="c in companies" :key="c.name" class="relative">
              <!-- Follow-up left border indicator -->
              <div v-if="isFollowUpDue(c)" class="absolute left-0 top-1 bottom-1 w-0.5 rounded-full bg-amber-400 z-10"/>

              <div @click="router.push(`/company/${encodeURIComponent(c.name)}`)"
                class="flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-all"
                :class="[
                  stageUpdating === c.name ? 'opacity-50 pointer-events-none' : '',
                  isFollowUpDue(c)
                    ? 'bg-amber-500/5 border-amber-500/20'
                    : 'bg-surface-raised border-border'
                ]">

                <CompanyAvatar :name="c.name" size="sm"/>

                <div class="flex-1 min-w-0">
                  <div class="flex items-start justify-between gap-2">
                    <div class="min-w-0">
                      <div class="flex items-center gap-1.5 min-w-0">
                        <p class="text-sm font-semibold text-text-primary truncate">{{ c.name }}</p>
                        <span v-if="c.has_report" class="w-1.5 h-1.5 rounded-full bg-emerald-500 flex-shrink-0" title="Has report"/>
                        <span v-if="(c.interviews?.length ?? 0) > 0" class="text-[10px] text-violet-400 flex-shrink-0 font-medium">{{ c.interviews.length }}✦</span>
                        <span v-if="isFollowUpDue(c)" class="text-[10px] text-amber-400 flex-shrink-0" title="Follow-up due">⏰</span>
                        <a v-if="c.job_url" :href="c.job_url" target="_blank" @click.stop
                          class="flex-shrink-0 text-text-muted hover:text-blue-400 transition-colors"
                          title="Open job posting">
                          <svg class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"/>
                          </svg>
                        </a>
                      </div>
                      <p v-if="roleStr(c.role)" class="text-xs text-text-muted truncate mt-0.5">{{ roleStr(c.role) }}</p>
                    </div>

                    <ApplicationRowActions :company="c" :open="activeActionRow === c.name"
                      @toggle="toggleActions(c.name)"
                      @open-detail="openDetail(c)"
                      @open-url="openUrl(c)"
                      @copy-email="copyEmail(c.recruiter.email)"
                      @quick-stage="(stage) => quickStage(c, stage)"/>
                  </div>

                  <div class="flex items-center flex-wrap gap-x-3 gap-y-1.5 mt-2">
                    <div class="flex items-center gap-1.5">
                      <span :class="['w-1.5 h-1.5 rounded-full flex-shrink-0', STAGE_COLORS[c.stage as ApplicationStage]?.dot]"/>
                      <span :class="['text-xs font-medium', STAGE_COLORS[c.stage as ApplicationStage]?.text]">{{ c.stage }}</span>
                    </div>
                    <span v-if="fmtSalary(c)" class="text-xs font-mono text-text-secondary">{{ fmtSalary(c) }}</span>
                    <span class="text-xs text-text-muted font-mono">{{ c.applied_date || (c.synced_at ? c.synced_at.slice(0,10) : '—') }}</span>
                    <RecommendBadge :recommend="c.recommend" size="sm"/>
                  </div>

                  <div v-if="c.match_pct !== null" class="flex items-center gap-1.5 mt-2">
                    <div class="flex-1 h-1 bg-surface-overlay rounded-full overflow-hidden">
                      <div :class="['h-full rounded-full', matchPctBar(c.match_pct)]" :style="{ width: `${c.match_pct}%` }"/>
                    </div>
                    <span :class="['text-xs font-mono font-bold w-9 text-right flex-shrink-0', matchPctColor(c.match_pct)]">{{ c.match_pct }}%</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Pagination -->
        <div v-if="totalPages > 1" class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 mt-4 px-3">
          <p class="text-xs text-text-muted">
            {{ (page - 1) * pageSize + 1 }}–{{ Math.min(page * pageSize, totalCount) }} of {{ totalCount }} companies
          </p>
          <div class="flex items-center gap-1 overflow-x-auto">
            <button @click="goToPage(1)" :disabled="page === 1"
              class="px-2 py-1 text-xs rounded-md border border-border text-text-muted hover:text-text-primary hover:border-accent/50 disabled:opacity-30 disabled:pointer-events-none transition-colors flex-shrink-0">
              ««
            </button>
            <button @click="goToPage(page - 1)" :disabled="page === 1"
              class="px-2 py-1 text-xs rounded-md border border-border text-text-muted hover:text-text-primary hover:border-accent/50 disabled:opacity-30 disabled:pointer-events-none transition-colors flex-shrink-0">
              «
            </button>
            <template v-for="p in totalPages" :key="p">
              <button v-if="p === 1 || p === totalPages || (p >= page - 2 && p <= page + 2)"
                @click="goToPage(p)"
                class="px-2.5 py-1 text-xs rounded-md border transition-colors flex-shrink-0"
                :class="p === page ? 'border-accent bg-accent/10 text-accent font-medium' : 'border-border text-text-muted hover:text-text-primary hover:border-accent/50'">
                {{ p }}
              </button>
              <span v-else-if="p === page - 3 || p === page + 3" class="text-xs text-text-muted px-1 flex-shrink-0">...</span>
            </template>
            <button @click="goToPage(page + 1)" :disabled="page === totalPages"
              class="px-2 py-1 text-xs rounded-md border border-border text-text-muted hover:text-text-primary hover:border-accent/50 disabled:opacity-30 disabled:pointer-events-none transition-colors flex-shrink-0">
              »
            </button>
            <button @click="goToPage(totalPages)" :disabled="page === totalPages"
              class="px-2 py-1 text-xs rounded-md border border-border text-text-muted hover:text-text-primary hover:border-accent/50 disabled:opacity-30 disabled:pointer-events-none transition-colors flex-shrink-0">
              »»
            </button>
          </div>
        </div>
        <p v-else class="text-xs text-text-muted mt-4 px-3">{{ totalCount }} companies</p>
      </div>
    </div>
  </div>
</template>
